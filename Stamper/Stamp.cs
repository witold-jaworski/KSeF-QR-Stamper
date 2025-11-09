using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using iText.IO.Font.Constants;
using iText.IO.Font.Otf;
using iText.IO.Image;
using iText.Kernel.Font;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Action;
using iText.Kernel.Pdf.Canvas;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using QRCoder;

namespace Stamper
{


	//Metody związane bezpośrednio z modyfikacją pliku PDF i tworzeniem kodu QR
	internal partial class Program
	{

		//Przetwarza PDF
		//Argumenty:
		//	srcPath:	ścieżka do pliku PDF, który ma być "ostemplowany"
		//	dstPath:	ścieżka do wynikowego pliku PDF (jest w tej procedurze tworzony)
		//	QRtext:		wyrażenie, które ma być zakodowane w kodzie QR
		//	nrKSeF:		numer KSeF (może być "", gdy nieznany)
		//	lastPage:	opcjonalny. True, gdy umieszczać na ostatniej stronie 
		private static void Stamp(string srcPath, string dstPath, string[] item)
		{
			PdfReader reader = new PdfReader(srcPath);
			PdfWriter writer = new PdfWriter(dstPath);
			PdfDocument pdf = new PdfDocument(reader, writer);
			Document doc = new Document(pdf);

			PlaceQR("INVOICE", pdf, doc, item[1], item[2]); //Kod QR I - zawsze
			if(item.Length > 3) 
							PlaceQR("CERTIFICATE",pdf, doc,	item[3], "CERTYFIKAT"); //Kod QR II - gdy OFFLINE.
			doc.Close();
			pdf.Close();
		}

		//Przetwarza PDF
		//Argumenty:
		//	srcPath:	ścieżka do pliku PDF, który ma być "ostemplowany"
		//	dstPath:	ścieżka do wynikowego pliku PDF (jest w tej procedurze tworzony)
		//	QRtext:		wyrażenie, które ma być zakodowane w kodzie QR
		//	nrKSeF:		numer KSeF (może być "", gdy nieznany)
		//	lastPage:	opcjonalny. True, gdy umieszczać na ostatniej stronie 
		private static void PlaceQR(string QrType, PdfDocument pdf, Document doc, string QRtext, string label)
		{
			int x;
			int y;
			int page = 1;
			bool lastPage = (Cnf($"{QrType}/last-page") == "1");

			ImageData code = CreateQRCode(QRtext);

			float QRsize = Int32.Parse(Cnf($"{QrType}/QR/size"));
			x = Int32.Parse(Cnf($"{QrType}/QR/x"));
			y = Int32.Parse(Cnf($"{QrType}/QR/y"));

			if (lastPage) page = pdf.GetNumberOfPages();
			var box = pdf.GetPage(page).GetPageSize();

			if (label != "")
			{
				int points = Int32.Parse(Cnf($"{QrType}/label/points"));
				int yoffset = Int32.Parse(Cnf($"{QrType}/label/yoffset"));
				string font = Cnf($"{QrType}/label/font");

				Paragraph par = new Paragraph();
				par.SetFont(PdfFontFactory.CreateFont(font));
				par.SetFontSize(points);
				par.SetFontColor(iText.Kernel.Colors.ColorConstants.BLACK);
				par.SetFixedPosition(page, x, box.GetHeight() - (y + QRsize + points + yoffset), QRsize);
				par.SetTextAlignment(TextAlignment.CENTER);
				par.Add(label);
				par.SetProperty(Property.ACTION, PdfAction.CreateURI(QRtext));
				doc.Add(par);
			}

			PdfCanvas overlay = new PdfCanvas(pdf, page);

			overlay.AddImageFittedIntoRectangle(code, new iText.Kernel.Geom.Rectangle(x, box.GetHeight() - y - QRsize, QRsize, QRsize), false);
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
			return ImageDataFactory.Create(img, null);
		}
	}
}
