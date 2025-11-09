# KSeF Stamper

Narzędzie linii poleceń, nanoszące na dokumenty PDF faktur kody QR wymagane przez Krajowy System e-Faktur (KSeF). Przystosowane do wsadowego przetwarzania serii dokumentów. Stworzone dla .NET Framework w wersji 4.6.1 lub wyższych. Wykorzystuje popularną bibliotekę iText.
Patrz [szczegółowy opis](Stamper/Readme.md).
Spis treści:

<!--TOC-->

* [Argumenty linii poleceń](#argumenty-linii-polecen)
* [Plik konfiguracji programu](#plik-konfiguracji-programu)
* [Format pliku wejściowego](#format-pliku-wejsciowego)
* [Kronikowanie "krok po kroku"](#kronikowanie-krok-po-kroku)

<!--/TOC-->

## Argumenty linii poleceń

* **<ścieżka do pliku z danymi wejściowymi>**.   
  To jedyny wymagany argument. Ścieżkę można także podawać względem położenia *Stamper.exe*. Opis formatu tego pliku - patrz [poniżej](#format-pliku-wejsciowego).
* **config**   
  Opcjonalny. Ścieżka do alternatywnego pliku \**.xml* z konfiguracją programu. Może być podana względem *Stamper.exe*. Szczegóły - por. sekcja ["Plik konfiguracji"](#plik-konfiguracji-programu).
* **options**   
  Opcjonalny. Flagi programu. Jeżeli podajesz więcej niż jedną - rozdziel je średnikiem. Aktualnie jest tylko jedna flaga - **verbose**. Włącza wyświetlanie w konsoli komunikatu o przetworzeniu każdej z faktur.
* **sid**   
  Opcjonalny. Unikalny identyfikator tej sesji ("sid" to skrót od "Session ID"). Stworzony specjalnie dla komponentu śledzącego "krok po kroku" wykonywanie zadania wsadowego (por. [poniżej](#kronikowanie-krok-po-kroku)).

Przykładowe wywołanie programu:

```
stamper.exe "..\\..\\invoice-list.txt" options verbose
```

## Plik konfiguracji programu

Domyślnie plik konfiguracji programu nazywa się **Stamper.xml** i jest umieszczony w tym samym folderze, co *Stamper.exe*. Za pomocą argumentu linii poleceń **config** możesz wskazać inny plik.

Główny element struktury XML konfiguracji nazywa się **config**. Zawiera ustawienia ogólne, oraz dwie sekcje: **INVOICE** i **CERTIFICATE**, określające sposób umieszczenia odpowiednich kodów QR na fakturze. Na przykład:

```xml
<?xml version="1.0" encoding="utf-8"?>
<!-- Plik konfiguracyjny dla programu Stamper.exe  -->
<config>
	<!-- katalogi programu. NIGDY nie wstawiaj na końcu "\\": -->
	<source>..\\..\\Testy\\pdfs</source> <!-- katalog ze źródłowymi plikami PDF -->
	<result>..\\..\\Testy\\results</result> <!-- katalog na pliki "ostemplowane" kodam QR -->
	<stepfiles>..\\..\\Testy\\logs</stepfiles> <!-- opcjonalny: katalog do szczegółowej komunikacji z programem wywołującym -->
	
	<!-- opcjonalnie: enkodowanie pliku wejściowego (wpisz, jeżeli nie jest to utf-8) -->
	<encoding>windows-1250</encoding>

	<INVOICE>
	...
	</INVOICE>
	<CERTIFICATE>
	...
	</CERTIFICATE>
</config>
```

Elementy obowiązkowe **config** to:

* **source** to folder ze źródłowymi plikami PDF ("do ostemplowania" kodami QR).
* **result** to folder na przetworzone ("ostemplowane") pliki PDF.
* sekcje **INVOICE** i **CERTIFICATE**.

Składnia sekcji **INVOICE** i **CERTIFICATE** jest identyczna. Opisują położenie i rozmiar kodu QR na pierwszej lub ostatniej stronie dokumentu PDF. Poniżej kodu QR znajduje się etykieta, o podanej w konfiguracji czcionce. Poniżej przykład definicji wymaganego przez KSeF kodu QR:

```xml
<!-- wszelkie współrzędne są w punktach, czyli 1/72 cala (to około 0.35mm). 
	Polożenie jest podawane dla lewego górnego narożnika obszaru (prostokąta obejmujacego tekst, obrazu kodu QR)
	Punkt 0,0 jest w lewym górnym narożniku strony.	Rozmiar pionowej strony A4 to 595x841 pt. 
	Wartości w pt podawaj  zawsze jako liczby całkowite (tak ustawiłem parser tego pliku)
	-->
	<INVOICE>
		<last-page>0</last-page> <!-- 0, jeżeli na pierwszej stronie, 1 jeżeli na ostatniej -->
		<QR>
			<!-- Kwadratowy kod QR -->
			<x>80</x>
			<y>65</y>
			<size>110</size>
			<!-- QR code jest kwadratem. Ten rozmiar obejmuje także 5% białe marginesy wokół kwadratu kodu -->
		</QR>
		<label>
			<!-- Numer KSEF / OFFLINE -->
			<font>Helvetica-Bold</font>
			<!-- Najlepiej uzywaj nazw standardowych fontów PDF: "Helvetica", "Courier", "Times-Roman", ... 
										Możliwe są także ich "mutacje": "Helvetica-Bold", "Courier-Bold", "Times-Bold". 
										Uwaga: jest różnica w nazwach czcionek pochylonych:. 
										Są: "Courier-Oblique", "Helvetica-Oblique" i "Times-Italic" -->
			<points>8</points> 	<!-- rozmiar czcionki -->
			<yoffset>15</yoffset>
			<!-- dodatkowe przesunięcie tekstu do dołu. Dostosuj do czcionki i ewentualnego zawinięcia tekstu -->
		</label>
	</INVOICE>
```

Sekcja **CERTIFICATE** opisuje drugi kod QR, który ma być umieszczany tylko dla faktu wystawionych w trybie OFFLINE. Jej składnia jest identyczna jak **INVOICE**. (Pamiętaj tylko, że kody QR certyfikatów powinny być większe, bo ich url są dużo dłuższe).

## Format pliku wejściowego

Plik tekstowy, w którym każda linia zawiera następujące elementy rozdzielone znakiem tabulacji (ASCII #9):

* **nazwa pliku pdf**   
  Nazwa z rozszerzeniem, ale bez ścieżki. (Program zakłada, że jest to **source** z pliku konfiguracji).
* **url linku do weryfikacji faktury**   
  (np. uzyskany z odp. wywołania biblioteki KSeF.Client MF)
* **numer KSEF**   
  lub, gdy jest jeszcze nieznany, stały tekst "OFFLINE".
* Opcjonalnie: **url do certyfikatu Wystawcy faktury**   
  Należy go podać, jeżeli poprzedni element jest "OFFLINE". (Ten url można uzyskać np. z odp. wywołania biblioteki KSeF.Client MF).

Przykładowa zawartość:

```
FA\_3\_Przykład\_1.pdf	https://ksef-test.mf.gov.pl/client-app/invoice/9999999999/15-02-2026/X-wQflQVyRs1fImlI-xDPnWk1V37hys4O1lkx9iJbSA	9999999999-20230908-8BEF280C8D35-4D
FA\_3\_Przykład\_10.pdf	https://ksef-test.mf.gov.pl/client-app/invoice/9999999999/15-02-2026/s53ueEjp1GsSNo6JQsCrJqaAtZAol23eNPG8VHm5NQQ	9999999999-20230908-8BEF280C8D35-4D
FA\_3\_Przykład\_11.pdf	https://ksef-test.mf.gov.pl/client-app/invoice/9999999999/15-03-2026/Kcu1TWt83sGAdGnK9MW4zbQhUAriO3jTgc\_Uml456NE	OFFLINE	https://ksef-test.mf.gov.pl/client-app/certificate/Nip/1234567890/9999999999/2B10A313FDA65344/Kcu1TWt83sGAdGnK9MW4zbQhUAriO3jTgc\_Uml456NE/2r9v45n1QHbJKsJqJOFWLp\_TM67yOgMchIbw9Zc\_vY8FFz\_33obo-pSEJfCaMIXyPgIYjfQwH3WgGiWxX\_wd0A
```

## Kronikowanie "krok po kroku"

Opcjonalnie program umożliwia raportowanie (logowanie) operacji "krok po kroku". Ta funkcja może być wykorzystywana przez program wywołujący np. do wyświetlania użytkownikowi postępu przetwarzania jakiejś długiej listy faktur.

Przed jej uruchomieniem należy przygotować folder na tymczasowe katalogi sesji programu. Wpisz jego ścieżkę w pliku konfiguracyjnym w opcjonalnym elemencie **stepfiles**:

```xml
<?xml version="1.0" encoding="utf-8"?>
<!-- Plik konfiguracyjny dla programu Stamper.exe  -->
<config>
	...
	<stepfiles>..\\..\\Testy\\logs</stepfiles> <!-- opcjonalny: katalog do szczegółowej komunikacji z programem wywołującym -->
	...
```

Gdy ta informacja jest wpisana, możesz wywołać program z dodatkowym argumentem sid ("session ID"). To nazwa tymczasowego katalogu na pliki kroków, który program wywołujący utworzył w folderze **stepfiles**. Powinien to być jakiś unikalny tekst/liczba. Dobrym kandydatem jest np. aktualna liczba milisekund od 1 stycznia 2025, czy coś w tym rodzaju:

```
stamper.exe "..\\..\\invoice-list.txt" sid 838338
```

Tak wywołany Stamper po przetworzeniu każdej faktury wpisuje w folder *{stepfiles}\\838338\* mały plik tekstowy {Nazwa pliku faktury}.done*\* lub *{Nazwa pliku faktury}*<b>.fail</b>. W środku jest jedna-dwie linie tekstu, opisująca wynik przetworzenia tego dokumentu.

Rozszerzenie \***.done** oznacza, że faktura została poprawnie "ostemplowana".

Rozszerzenie \***.fail** sygnalizuje, że "ostemplowanie" się nie powiodło. (W środku pliku zapewne są jakieś szczegóły).

Program wywołujący może w trakcie działania Stamper usuwać z katalogu tymczasowego pliki *.done* i *.fail*, które już odczytał.

Gdy Stamper zakończy działanie, program wywołujący powinien usunąć cały katalog *sid* (w przykładzie powyżej to *\\838338*), aby nie pozostawiać po sobie "śmieci".

