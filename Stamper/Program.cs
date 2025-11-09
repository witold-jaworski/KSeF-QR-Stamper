using System;
using System.Collections.Generic;
using System.Configuration;
using System.Drawing;
using System.IO;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Xml;


namespace Stamper
{
	internal partial class Program
	{
		private const string ScreenHeader = "Program nanoszący kody QR faktur KSeF na wskazane pliki PDF. Wersja {0}. Autor: Witold Jaworski, 2023-2025.\nUdostępniony na licencji AGPLv3 (por. https://www.gnu.org/licenses/agpl-3.0.txt).";

		private const string UsageDetails = @"
			Argumenty :

			'<cmd>' [config 'ścieżka do alterantywnego pliku *.xml z konfiguracją'] [options flaga[;flaga[;flaga]]] [sid 'identyfikator']
			
			gdzie:
			<cmd>    - scieżka do plik poleceń. Opis formatu - patrz poniżej. To musi być pierwszy argument programu
			config   - opcja, pozwalająca wskazać alternatywny plik *.xml z konfiguracją programu
			options  - opcjonalna lista pojedynczych flag, rozdzielanych średnikami.
			sid      - opcja, unikalny identyfikator tego przebiegu. Wykorzystywany przez program wywołujący 
			           do śledzenia postępu zadania.

			Ścieżki można podawać jako absolutne lub względne (względem pliku *.exe). 
			Można w nich stosować nazwy zmiennych środowiskowych, np %Temp% albo %AppData%
			Jeżeli zawierają spacje - można je ująć w cudzysłów ('..').
			
			Przykładowe wywołanie:

			Stamper.exe '../../Inbox/cmd.txt' options verbose sid 23493949

			Plik poleceń programu to plik tekstowy rozdzielany znakami tabulacji, w którym każda linia zawiera polecenie 
			'ostemplowania' kodami QR pojedynczego pliku PDF. Zawartość pojedynczej linii pliku poleceń:
			<Plik PDF>	- nazwa pliku *.pdf, BEZ ścieżki (ta jest podana jako <source> w pliku konfiguracji)
			<url QR I>	- tekst linku do zakodowania w kodzie QR faktury ('kod QR I')
			<nr KSeF> | OFFLINE - numer KSeF nadany fakturze, lub stały tekst 'OFFLINE', gdy go nie ma.
			[url QR II] - występuje tylko wtedy, gdy poprzedni element w wierszu = 'OFFLINE'.
			              Tekst linku do zakodowania w kodzie QR certyfikatu ('kod QR II')
			";
		
		private const int PixelsPerQRdot = 10; //liczba pikseli na elementarny kwadrat kodu QR - im większa, tym lepsza "skalowalność" obrazu i nieco większy rozmiar pliku
		private const string ConfigFileName = "Stamper.xml"; //ścieżka do domyślnego pliku z konfiguracją programu

		//teksty, które występują w więcej niż jednym miejscu lepiej mieć jako stałe:
		private const string F_VERBOSE = "verbose"; 

		//Pola klasy (efektywnie - zmienne globalne)
		private static readonly XmlDocument config = new XmlDocument();
		private static Args progArgs;
		private static StepfileLogger log;
		static int Main(string[] args)
		{
			Console.WriteLine(String.Format(ScreenHeader, ProgramVersion()));
			//Jak nie ma pierwszego argumentu - to przedstaw się i do widzenia.
			if (args.Length < 1)
			{
				Console.WriteLine(UsageDetails.Replace("\t", " ").Replace("'","\"")); //tabulacje na spacje, aby powstał margines od krawędzi okna CLI
				return -1;
			}

			try
			{
				progArgs = new Args(args); //Najpierw porzadek z argumentami:
				//Uwzględnij ewentualną zmianę tej ścieżki w argumentach programu:
				if(!progArgs.TryGetValue("config", out string	configPath))
															configPath = AppContext.BaseDirectory + ConfigFileName;

				if (!progArgs.TryGetValue("source", out string srcPath))
															srcPath = Cnf("source");

				if (!progArgs.TryGetValue("result", out string dstPath))
															srcPath = Cnf("result");

				//ewentualne opcje programu z linii poleceń (mogą być puste)
				if (!progArgs.TryGetValue("options", out string options)) options = "";
				options = options.Trim().ToLower();
				
				//ewentualny identyfikator sesji dla loggera:
				if (!progArgs.TryGetValue("sid", out string sid)) sid = "";

				//Załaduj konfigurację..
				config.Load(FullPath(configPath));


				//Zainicjalizuj logger
				log = new StepfileLogger(Cnf("stepfiles",""), sid, Cnf("encoding", "utf-8"));

				//Przetwórz plik z poleceniami:
				srcPath = FullPath(srcPath);
				dstPath = FullPath(dstPath);
				var commands = File.ReadAllLines(FullPath(progArgs["cmd"]), Encoding.GetEncoding(Cnf("encoding", "utf-8")));
				foreach (var line in commands)
				{
					var item = line.Split('\t'); //Pojedyncza linia, podzielona na elementy
					try
					{
						Stamp($"{srcPath}\\{item[0]}", $"{dstPath}\\{item[0]}", item);
						if (options.Contains(F_VERBOSE)) Console.WriteLine($"Oznaczono: {item[0]}\t{item[2]}");
						log.LogSuccess(item[0],$"Oznaczono kodem QR dla '{item[2]}'");
					}
					catch (Exception e) 
					{
						log.LogFailure(item[0],e.Message);
						Console.WriteLine($"\nBłąd podczas przetwarzania '{item[0]}':\n{e}\n{e.StackTrace}\n\nKontynuacja przetwarzania...\n");
					}
				}
				if (options.Contains(F_VERBOSE)) Console.WriteLine("Przetwarzanie zakończone");
				return 0;
			}
			catch (Exception e) 
			{
				Console.WriteLine("Błąd krytyczny podczas przetwarzania:");
				Console.WriteLine(e);
				return -1;	
			}
		}
		//Zwraca numer wersji programu
		private static string ProgramVersion()
		{
			System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
			System.Diagnostics.FileVersionInfo info = System.Diagnostics.FileVersionInfo.GetVersionInfo(assembly.Location); //GetVersionInfo(assembly.CodeBase.Replace("file:///", ""))
			return info.FileVersion.ToString();
		}

		//Zwraca zawsze pełną ścieżkę - do rozwinięcia ew. ścieżek względem położenia programu
		//Argumenty:
		//  path: ścieżka, która może być względna
		//Ścieżki względne "urealnia" od folderu programu
		static public string FullPath(string path)
		{
			if (path.EndsWith("\\")) path = path.Substring(0, path.Length - 1); //Usuń ewentualny backslash z końca
			path = Environment.ExpandEnvironmentVariables(path);    //Aby można było stosować %temp%
			if (System.IO.Path.IsPathRooted(path) == false) path = AppContext.BaseDirectory + path;
			return Path.GetFullPath(path);
		}

		//Lista argumentów programu
		internal class Args : Dictionary<string, string> //Skrót, by nie pisał długich list argumentów
		{
			public Args(string[] values) : base()
			{
				this.Add("cmd", values[0]); //Ścieżka do pliku poleceń zawsze będzie pod kluczem "cmd"
				//Pozostałe argumenty musza mieć nazwy:
				for (int i = 1; i < values.Length - 1; i += 2)
				{
					this.Add(values[i], values[i + 1]);
				}
			}
		}

		//Skrót do odwoływania się do pól konfiguracji programu
		//Argumenty:
		//  xpath: wyrażenie xpath, wybierające pojedynczy element
		//	defaultValue: jeżeli podany (!= null), funkcja nie zgłasza błędu tylko podaje tę wartość
		//Procedura zgasza wyjątek, gdy nie znalazła wskazanego węzła
		static private string Cnf(string xpath, string defaultValue = null)
		{
			var result = config.DocumentElement.SelectSingleNode(xpath);
			if (result == null)
			{
				if (defaultValue == null)
				{
#pragma warning disable CS0618 // Jakiś błędne ostrzeżenie o przestarzałej klasie - a klasa podana jako następca nie istnieje
					throw new ConfigurationException("Could not find node '" + xpath + "' in the configuration file ('" + ConfigFileName + "')");
#pragma warning restore CS0618 // Type or member is obsolete
				}
				else  
					return defaultValue; 
			}
			else
				return result.InnerText;
		}

	}
}
