using System;
using System.Collections.Generic;
using System.Configuration;
using System.Drawing;
using System.IO;
using System.Xml;
using iText.IO.Font.Constants;
using iText.IO.Image;
using iText.Kernel.Font;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas;
using iText.Layout;
using iText.Layout.Element;
using QRCoder;

namespace Stamper
{
	internal class Program
	{
		private const string ScreenHeader = "Program nanoszący kod QR i numer KSeF faktury na wskazany plik PDF. Wersja {0}. Autor: Witold Jaworski, 2023.\nUdostępniony na licencji AGPLv3 (por. https://www.gnu.org/licenses/agpl-3.0.txt).";

		private const string UsageDetails = @"
			Argumenty:

			cert 'srcPDF' 'dstPDF' 'xmlFile' [KSeF] 

			gdzie:
			cert       - typ certyfikatu: prod|demo|test 
			             ('prod' to certyfikat dla serwera produkcyjnego, 'test' - testowego).
			'srcPDF'   - ścieżka do źródłowego pliku PDF faktury, który ma być zmodyfikowany.
			'dstPDF'   - ścieżka do docelowego pliku PDF faktury, który ma powstać 
			             (czyli do rezultat programu).
			'xmlFile'  - ścieżka do pliku XML z faktura ustrukturyzowaną 
			             (potrzebna do wyliczania tekstu na kod QR)
			KSeF       - opcjonalny. Numer KSeF, nadany fakturze 
			             (pomiń, jeżeli nadajesz kod QR awaryjnie, bez dostępu do KSeF)
			
			Ścieżki można podawać jako absolutne lub względne (względem pliku *.exe). 
			Można w nich stosować nazwy zmiennych środowiskowych, np %Temp% albo %AppData%
			Jeżeli zawierają spacje - można je ująć w cudzysłów ('..').
			
			Przykładowe wywołanie:

			Stamper.exe test '../../Inbox/FV1234 2023.pdf' '../../Stamped/FV1234 2023.pdf' '../../Accepted/FV1234 2023.xml' 5251469286-20230208-2E5704-7B37EA-67

			";
		
		private const int PixelsPerQRdot = 10; //liczba piksli na elementarny kwadrat kodu QR
		private const string CertTypes = "prod,demo,test"; //dopuszczalne typy certyfikatów
		private const string ConfigFileName = "Stamper.xml"; //Nazwa pliku z konfiguracją programu
		private static readonly XmlDocument config = new XmlDocument();
		static int Main(string[] args)
		{
			string mode;
			string srcPath;
			string dstPath = null;
			string xmlPath;
			//W poniższych liniach przypisania testowe:
			string nrKSeF = "5251469286-20230208-2E5704-7B37EA-67"; 
			string QRText = "https://www.podatki.gov.pl/ksef/API/common/QRCodeInvoiceViewer?NIP=1111111111&cert=2QfHxdbZx8XU1YWmQMOFmaNA46iXhUBgQOKFmUB7QPDw&sha2=o1WIn2qLtTyXPD5PI78/RmvV3at6dfbBmdibRd/pBjs=";

			Console.WriteLine(String.Format(ScreenHeader, ProgramVersion()));
			//Jak nie ma pierwszego argumentu - to przedstaw się i do widzenia.
			if (args.Length < 4)
			{
				Console.WriteLine(UsageDetails.Replace("\t", " ").Replace("'","\"")); //tabulacje na spacje, aby powstał margines od krawędzi okna CLI
				return -1;
			}

			try
			{
				//Załaduj konfigurację:
				config.Load(AppContext.BaseDirectory + ConfigFileName);

				mode = args[0].ToLower();
				if (!CertTypes.Contains(mode)) throw new ArgumentException(String.Format("Pierwszy argument ('{0}') może tylko mieć jedną z wartości: {1}", mode, CertTypes));
				srcPath = FullPath(args[1]);
				dstPath = FullPath(args[2]);
				xmlPath = FullPath(args[3]);
				if (args.Length > 4) nrKSeF = args[4];	

				Stamp(srcPath, dstPath, QRText, nrKSeF);
				Console.ReadLine();
				return 0;
			}
			catch (Exception e) 
			{
				Console.WriteLine("Błąd podczas przetwarzania:");
				Console.WriteLine(e);
				//Nie zostawiaj śmieci po sobie!
				if (dstPath!=null && File.Exists(dstPath)) File.Delete(dstPath);
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

		//Zwraca zawsze pełną ścieżkę
		//Argumenty:
		//  path: ścieżka, która może być względna
		//Ścieżki względne "urealnia" od folderu programu
		static public string FullPath(string path)
		{
			path = Environment.ExpandEnvironmentVariables(path);    //Aby można było stosować %temp%
			if (System.IO.Path.IsPathRooted(path)) return path;
			else return AppContext.BaseDirectory + path;
		}

		internal class Args : Dictionary<string, string> //Skrót, by nie pisał długich list argumentów
		{ }

		//Skrót do odwoływania się do pól konfiguracji programu
		//Argumenty:
		//  xpath: wyrażenie xpath, wybierające pojedynczy element
		//Procedura zgasza wyjątek, gdy nie znalazła wskazanego węzła
		static private string Cnf(string xpath, Args args = null)
		{
			var result = config.DocumentElement.SelectSingleNode(xpath);
			if (result == null)
			{
#pragma warning disable CS0618 // Jakiś błędne ostrzeżenie o przestarzałej klasie - a klasa podana jako następca nie istnieje
				throw new ConfigurationException("Could not find node '" + xpath + "' in the configuration file ('" + ConfigFileName + "')");
#pragma warning restore CS0618 // Type or member is obsolete
			}
			else
			{
				string expr = result.InnerText;
				if (args != null)
				{
					foreach (string key in args.Keys)
					{
						expr = expr.Replace(key, args[key]);
					}
				}
				return expr;
			}
		}

		//Przetwarza PDF
		//Argumenty:
		//	srcPath:	ścieżka do pliku PDF, który ma być "ostemplowany"
		//	dstPath:	ścieżka do wynikowego pliku PDF (jest w tej procedurze tworzony)
		//	QRtext:		wyrażenie, które ma być zakodowane w kodzie QR
		//	nrKSeF:		numer KSeF (może być null)
		private static void Stamp(string srcPath, string dstPath, string QRtext, string nrKSeF)
		{
			int x;
			int y;

			ImageData code = CreateQRCode(QRtext);

			PdfReader reader = new PdfReader(srcPath);
			PdfWriter writer = new PdfWriter(dstPath);
			PdfDocument pdf = new PdfDocument(reader, writer);

			PdfCanvas overlay = new PdfCanvas(pdf, 1);
			var box = pdf.GetPage(1).GetPageSize();
			if (nrKSeF != null)
			{
				x = Int32.Parse(Cnf("label/x"));
				y = Int32.Parse(Cnf("label/y"));
				int points = Int32.Parse(Cnf("label/points"));
				string font = Cnf("label/font");

				overlay.BeginText();
				overlay.SetFontAndSize(PdfFontFactory.CreateFont(font), points); //StandardFonts.HELVETICA = "Helvetica", inne to: "Courier", 
				overlay.MoveText(x, box.GetHeight() - y - points); //w iText podajemy lewy DOLNY narożnik tekstu, a w pliku konfiguracji założyłem, że jest to lewy GÓRNY
				overlay.ShowText("Nr KSeF: " + nrKSeF);
				overlay.EndText();
			}
	
			x = Int32.Parse(Cnf("QR/x"));
			y = Int32.Parse(Cnf("QR/y"));
			float QRsize = Int32.Parse(Cnf("QR/size"));

			overlay.AddImageFittedIntoRectangle(code, new iText.Kernel.Geom.Rectangle(x, box.GetHeight() - y - QRsize, QRsize, QRsize), false);

			pdf.Close();
		}

		//Zwraca tekst przetworzony na kod QR w postaci obrazu iText
		//Argumenty:
		//	text:	tekst, który ma być zakodowany w kodzie QR
		//Procedura pomocnicza, dodana dla większej czytelności kodu
		private static ImageData CreateQRCode(string text) 
		{

			QRCodeGenerator codeGenerator = new QRCodeGenerator();
			QRCodeData codeData = codeGenerator.CreateQrCode(text, QRCodeGenerator.ECCLevel.Q);
			QRCode code = new QRCode(codeData);
			System.Drawing.Image img = code.GetGraphic(PixelsPerQRdot);
			return ImageDataFactory.Create(img,null);
		} 


	}
}
