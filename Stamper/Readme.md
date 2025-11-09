# KSeF Stamper

Narzêdzie linii poleceñ, nanosz¹ce na dokumenty PDF faktur kody QR wymagane przez Krajowy System e-Faktur (KSeF). Przystosowane do wsadowego przetwarzania serii dokumentów. Stworzone dla .NET Framework w wersji 4.6.1 lub wy¿szych. Wykorzystuje popularn¹ bibliotekê iText.

Spis treœci:

<!--TOC-->

* [Argumenty linii poleceñ](#argumenty-linii-polecen)
* [Plik konfiguracji programu](#plik-konfiguracji-programu)
* [Format pliku wejœciowego](#format-pliku-wejsciowego)
* [Kronikowanie "krok po kroku"](#kronikowanie-krok-po-kroku)

<!--/TOC-->

## Argumenty linii poleceñ
<a name="argumenty-linii-poleceñ"></a>
* **<œcie¿ka do pliku z danymi wejœciowymi>**.   
  To jedyny wymagany argument. Œcie¿kê mo¿na tak¿e podawaæ wzglêdem po³o¿enia *Stamper.exe*. Opis formatu tego pliku - patrz [poni¿ej](#format-pliku-wejsciowego).
* **config**   
  Opcjonalny. Œcie¿ka do alternatywnego pliku \**.xml* z konfiguracj¹ programu. Mo¿e byæ podana wzglêdem *Stamper.exe*. Szczegó³y - por. sekcja ["Plik konfiguracji"](#plik-konfiguracji-programu).
* **options**   
  Opcjonalny. Flagi programu. Je¿eli podajesz wiêcej ni¿ jedn¹ - rozdziel je œrednikiem. Aktualnie jest tylko jedna flaga - **verbose**. W³¹cza wyœwietlanie w konsoli komunikatu o przetworzeniu ka¿dej z faktur.
* **sid**   
  Opcjonalny. Unikalny identyfikator tej sesji ("sid" to skrót od "Session ID"). Stworzony specjalnie dla komponentu œledz¹cego "krok po kroku" wykonywanie zadania wsadowego (por. [poni¿ej](#kronikowanie-krok-po-kroku)).

Przyk³adowe wywo³anie programu:

```
stamper.exe "..\..\invoice-list.txt" options verbose
```

## Plik konfiguracji programu

Domyœlnie plik konfiguracji programu nazywa siê **Stamper.xml** i jest umieszczony w tym samym folderze, co *Stamper.exe*. Za pomoc¹ argumentu linii poleceñ **config** mo¿esz wskazaæ inny plik.

G³ówny element struktury XML konfiguracji nazywa siê **config**. Zawiera ustawienia ogólne, oraz dwie sekcje: **INVOICE** i **CERTIFICATE**, okreœlaj¹ce sposób umieszczenia odpowiednich kodów QR na fakturze. Na przyk³ad:

```xml
<?xml version="1.0" encoding="utf-8"?>
<!-- Plik konfiguracyjny dla programu Stamper.exe  -->
<config>
	<!-- katalogi programu. NIGDY nie wstawiaj na koñcu "\": -->
	<source>..\..\Testy\pdfs</source> <!-- katalog ze Ÿród³owymi plikami PDF -->
	<result>..\..\Testy\results</result> <!-- katalog na pliki "ostemplowane" kodam QR -->
	<stepfiles>..\..\Testy\logs</stepfiles> <!-- opcjonalny: katalog do szczegó³owej komunikacji z programem wywo³uj¹cym -->
	
	<!-- opcjonalnie: enkodowanie pliku wejœciowego (wpisz, je¿eli nie jest to utf-8) -->
	<encoding>windows-1250</encoding>

	<INVOICE>
	...
	</INVOICE>
	<CERTIFICATE>
	...
	</CERTIFICATE>
</config>
```

Elementy obowi¹zkowe **config** to:

* **source** to folder ze Ÿród³owymi plikami PDF ("do ostemplowania" kodami QR).
* **result** to folder na przetworzone ("ostemplowane") pliki PDF.
* sekcje **INVOICE** i **CERTIFICATE**.

Sk³adnia sekcji **INVOICE** i **CERTIFICATE** jest identyczna. Opisuj¹ po³o¿enie i rozmiar kodu QR na pierwszej lub ostatniej stronie dokumentu PDF. Poni¿ej kodu QR znajduje siê etykieta, o podanej w konfiguracji czcionce. Poni¿ej przyk³ad definicji wymaganego przez KSeF kodu QR:

```xml
<!-- wszelkie wspó³rzêdne s¹ w punktach, czyli 1/72 cala (to oko³o 0.35mm). 
	Polo¿enie jest podawane dla lewego górnego naro¿nika obszaru (prostok¹ta obejmujacego tekst, obrazu kodu QR)
	Punkt 0,0 jest w lewym górnym naro¿niku strony.	Rozmiar pionowej strony A4 to 595x841 pt. 
	Wartoœci w pt podawaj  zawsze jako liczby ca³kowite (tak ustawi³em parser tego pliku)
	-->
	<INVOICE>
		<last-page>0</last-page> <!-- 0, je¿eli na pierwszej stronie, 1 je¿eli na ostatniej -->
		<QR>
			<!-- Kwadratowy kod QR -->
			<x>80</x>
			<y>65</y>
			<size>110</size>
			<!-- QR code jest kwadratem. Ten rozmiar obejmuje tak¿e 5% bia³e marginesy wokó³ kwadratu kodu -->
		</QR>
		<label>
			<!-- Numer KSEF / OFFLINE -->
			<font>Helvetica-Bold</font>
			<!-- Najlepiej uzywaj nazw standardowych fontów PDF: "Helvetica", "Courier", "Times-Roman", ... 
										Mo¿liwe s¹ tak¿e ich "mutacje": "Helvetica-Bold", "Courier-Bold", "Times-Bold". 
										Uwaga: jest ró¿nica w nazwach czcionek pochylonych:. 
										S¹: "Courier-Oblique", "Helvetica-Oblique" i "Times-Italic" -->
			<points>8</points> 	<!-- rozmiar czcionki -->
			<yoffset>15</yoffset>
			<!-- dodatkowe przesuniêcie tekstu do do³u. Dostosuj do czcionki i ewentualnego zawiniêcia tekstu -->
		</label>
	</INVOICE>
```

Sekcja **CERTIFICATE** opisuje drugi kod QR, który ma byæ umieszczany tylko dla faktu wystawionych w trybie OFFLINE. Jej sk³adnia jest identyczna jak **INVOICE**. (Pamiêtaj tylko, ¿e kody QR certyfikatów powinny byæ wiêksze, bo ich url s¹ du¿o d³u¿sze).

## Format pliku wejœciowego
<a name="format-pliku-wejœciowego"></a>
Plik tekstowy, w którym ka¿da linia zawiera nastêpuj¹ce elementy rozdzielone znakiem tabulacji (ASCII #9):

* **nazwa pliku pdf**   
  Nazwa z rozszerzeniem, ale bez œcie¿ki. (Program zak³ada, ¿e jest to **source** z pliku konfiguracji).
* **url linku do weryfikacji faktury**   
  (np. uzyskany z odp. wywo³ania biblioteki KSeF.Client MF)
* **numer KSEF**   
  lub, gdy jest jeszcze nieznany, sta³y tekst "OFFLINE".
* Opcjonalnie: **url do certyfikatu Wystawcy faktury**   
  Nale¿y go podaæ, je¿eli poprzedni element jest "OFFLINE". (Ten url mo¿na uzyskaæ np. z odp. wywo³ania biblioteki KSeF.Client MF).

Przyk³adowa zawartoœæ:

```
FA\_3\_Przyk³ad\_1.pdf	https://ksef-test.mf.gov.pl/client-app/invoice/9999999999/15-02-2026/X-wQflQVyRs1fImlI-xDPnWk1V37hys4O1lkx9iJbSA	9999999999-20230908-8BEF280C8D35-4D
FA\_3\_Przyk³ad\_10.pdf	https://ksef-test.mf.gov.pl/client-app/invoice/9999999999/15-02-2026/s53ueEjp1GsSNo6JQsCrJqaAtZAol23eNPG8VHm5NQQ	9999999999-20230908-8BEF280C8D35-4D
FA\_3\_Przyk³ad\_11.pdf	https://ksef-test.mf.gov.pl/client-app/invoice/9999999999/15-03-2026/Kcu1TWt83sGAdGnK9MW4zbQhUAriO3jTgc\_Uml456NE	OFFLINE	https://ksef-test.mf.gov.pl/client-app/certificate/Nip/1234567890/9999999999/2B10A313FDA65344/Kcu1TWt83sGAdGnK9MW4zbQhUAriO3jTgc\_Uml456NE/2r9v45n1QHbJKsJqJOFWLp\_TM67yOgMchIbw9Zc\_vY8FFz\_33obo-pSEJfCaMIXyPgIYjfQwH3WgGiWxX\_wd0A
```

## Kronikowanie "krok po kroku"

Opcjonalnie program umo¿liwia raportowanie (logowanie) operacji "krok po kroku". Ta funkcja mo¿e byæ wykorzystywana przez program wywo³uj¹cy np. do wyœwietlania u¿ytkownikowi postêpu przetwarzania jakiejœ d³ugiej listy faktur.

Przed jej uruchomieniem nale¿y przygotowaæ folder na tymczasowe katalogi sesji programu. Wpisz jego œcie¿kê w pliku konfiguracyjnym w opcjonalnym elemencie **stepfiles**:

```xml
<?xml version="1.0" encoding="utf-8"?>
<!-- Plik konfiguracyjny dla programu Stamper.exe  -->
<config>
	...
	<stepfiles>..\..\Testy\logs</stepfiles> <!-- opcjonalny: katalog do szczegó³owej komunikacji z programem wywo³uj¹cym -->
	...
```

Gdy ta informacja jest wpisana, mo¿esz wywo³aæ program z dodatkowym argumentem sid ("session ID"). To nazwa tymczasowego katalogu na pliki kroków, który program wywo³uj¹cy utworzy³ w folderze **stepfiles**. Powinien to byæ jakiœ unikalny tekst/liczba. Dobrym kandydatem jest np. aktualna liczba milisekund od 1 stycznia 2025, czy coœ w tym rodzaju:

```
stamper.exe "..\..\invoice-list.txt" sid 838338
```

Tak wywo³any Stamper po przetworzeniu ka¿dej faktury wpisuje w folder *{stepfiles}\\838338\* ma³y plik tekstowy {Nazwa pliku faktury}.done*\* lub *{Nazwa pliku faktury}*<b>.fail</b>. W œrodku jest jedna-dwie linie tekstu, opisuj¹ca wynik przetworzenia tego dokumentu.

Rozszerzenie \***.done** oznacza, ¿e faktura zosta³a poprawnie "ostemplowana".

Rozszerzenie \***.fail** sygnalizuje, ¿e "ostemplowanie" siê nie powiod³o. (W œrodku pliku zapewne s¹ jakieœ szczegó³y).

Program wywo³uj¹cy mo¿e w trakcie dzia³ania Stamper usuwaæ z katalogu tymczasowego pliki *.done* i *.fail*, które ju¿ odczyta³.

Gdy Stamper zakoñczy dzia³anie, program wywo³uj¹cy powinien usun¹æ ca³y katalog *sid* (w przyk³adzie powy¿ej to *\\838338*), aby nie pozostawiaæ po sobie "œmieci".

