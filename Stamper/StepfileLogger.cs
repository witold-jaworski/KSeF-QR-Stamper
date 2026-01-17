using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stamper
{
	//Klasa służy do implementacji "cząstkowego raportowania postępu" poprzez małe pliki tekstowe 
	//umieszczane w folderze ustalonym przez Wywołującego, o nazwie przekazanej w argumentach programu
	//Współpracuje z klasą VBA StepwiseBatch
	internal class StepfileLogger
	{
		protected readonly string dirPath; //Ścieżka do katalogu, w którym należy umieszczać pliki
										   //pusta, jeżeli funkcja jest wyłączona.
		protected readonly Encoding enc;   //enkodowanie plików wynikowych	

		//Argumenty:
		//	stepfileDir:	folder na pliki z wynikami kolejnych kroków (może być "")
		//	sessionId:		unikalny identyfikator tej sesji (argument "id" linii poleceń programu)
		//	encoding:		symbol strony kodowej pliku wynikowego. Np. "windows-1250", albo "utf-8"
		public StepfileLogger(string stepfileDir, string sessionId, string encoding) 
		{
			dirPath = "";
			if (stepfileDir == "" || sessionId == "") return;
			stepfileDir = Program.FullPath(stepfileDir);
			var dir = Path.Combine(stepfileDir, sessionId); //taka miałaby byc ścieżka na tymczasowe pliki
			if (Directory.Exists(dir)) //Ścieżkę tymczasową tworzy i usuwa program wywołujący:
			{
				dirPath = dir;
				enc = Encoding.GetEncoding(encoding);
			}
		}

		//Zapisuje wiadomość message do pliku o podanej nazwie i rozszerzeniu zależnym od flagi isDone
		//Argumenty:
		//	fileName:	nazwa pliku wraz z domyślnym rozszerzeniem (taka, jaka figuruje w pliku poleceń)
		//	isDone:		true, jeżeli przetwarzanie zakończone pomyślnie
		//	message:	komunikat do wpisania do pliku
		protected void LogToFile(string fileName, bool isDone, string message)
		{
			if (dirPath == "") return; //Nie mamy nic do roboty
			fileName = Path.GetFileNameWithoutExtension(fileName) + (isDone? ".done" : ".fail");
			File.AppendAllText(Path.Combine(dirPath, fileName), message, enc);
		}

		//Zapisują wiadomość message do pliku o podanej nazwie i rozszerzeniu ".done"
		//Argumenty:
		//	fileName:	nazwa pliku wraz z domyślnym rozszerzeniem (taka, jaka figuruje w pliku poleceń)
		//	message:	komunikat do wpisania do pliku
		public void LogSuccess(string fileName, string message)
		{ LogToFile(fileName, true, message);}
		//Zapisują wiadomość message do pliku o podanej nazwie i rozszerzeniu ".fail"
		//Argumenty:
		//	fileName:	nazwa pliku wraz z domyślnym rozszerzeniem (taka, jaka figuruje w pliku poleceń)
		//	message:	komunikat do wpisania do pliku
		public void LogFailure(string fileName, string message)
		{ LogToFile(fileName, false, message); }
	}
}
