# Begriffe

## Objekt
Ein Objekt ist ein konkreter Wert, welcher das Ergebnis eines Ausdruckes oder
der Inhalt einer Variable ist.

## Symbol
Ein Symbol ist ein benannter Wert, welcher in einer Variablen oder einem Parameter
gespeichert wird.

## Scope
Die aktuelle Sicht auf alle definierten Symbole, welche von einer bestimmten Stelle
im Code adressiert werden können.

## Parameter und Argument
Eine Funktion besitzt in ihrer Definition eine Liste an benannten Parametern, welche die
abstrakte Definition liefern. Bei einem Funktionsaufruf wird ein Argument in einen Parameter
übergeben. Das heißt, dass ein Argument den Wert festlegt, welcher in einen Parameter übergeben
wird.

## Statisch
In Psi bedeutet "statisch", dass etwas zur Zeit der Compilierung bekannt ist und auch schon
ausgewertet werden kann.

## Feld
Ein Feld ist ein Bestandteil bzw. eine Eigenschaft eines Objektes. Felder können beispielsweise
Informationen über die Länge eines Objekts (`myarray.length`) enthalten oder aber bei Records
vom Benutzer festgelegte "Unterobjekte" sein.

# Operatoren:

## Zuweisung (`=`):
Bei de Ausführung der Operation `a = b` wird die Struktur des Objekts `b`
ohne Umformung in das Objekt `a` übertragen. Die beiden Objekte sind danach
als *gleich* anzusehen.

## Meta-Operator (`'field`):
Der Meta-Operator greift auf statische Informationen eines Symbols zu

## Feld-Operator (`.field`)
Der Feld-Operator greift auf die Felder eines Objektes zu.

## Heap-Operator (`new`):
Bei der Ausführung des Heap-Operators `new a` wird ein neuer Wert des Typs
von `a` auf dem Heap alloziert und mit den Werten aus `a` initialisiert.
Zurückgegeben wird ein Wert des Typs `ref<a'type>`.

# Speicherorte für Objekte

## Globaler Speicher
Alle Variablen, welche in einem Modul liegen, sind im globalen Speicher.

## Lokaler Speicher
Variablen, welche als lokale Variable oder aber als Parameter definiert sind, liegem im lokalen Speicher.

## Heap-Speicher
Das Ergebnis eines Ausdrucks kann in den Heap-Speicher für längerfristige, dynamische Speicherung "verschoben" werden.
Hierzu erhält der Wert eine einzigartige Speicheradresse (Pointer)

## Temporärer Speicher
Objekte, die bei der Auswertung eines Ausdrucks entstehen und dafür als Zwischenergebnis gespeichert werden müssen,
liegen im temporären Speicher. Dieser Speicher kann nicht referenziert werden.

# Funktionen

Funktionen in Psi sind spezielle Werte, welche aufgerufen werden können. Eine Funktion wird in einer Variable mit
einem Funktionstypen gespeichert.

```psi
// Definition einer Funktion, welche in einer Konstante gespeichert wird
const funktion = fn(i : int) -> string
{
	// Liefert einen String mit i horizontalen Tabulatoren zurück.
	return RepeatString("\t", i);
};

// Definition eines Funktionstypens
type funktionstyp = fn(i : int) -> string;

// Eine Funktionsvariable, welche eine Funktion enthält
var funktionsvariable : funktionstyp = funktion;

// Aufruf der Funktion über die Funktionsvariable
var ergebnis = funktionsvariable(10);

print(ergebnis);
```

## Funktionsaufrufe

Ein Funktionsaufruf legt ein Stackframe an, welches die Rücksprungadresse sowie die lokalen Variablen und Parameter
enthält. Anschließend wird der Code in der Funktion ausgeführt und ein einzelner Wert zurückgegeben.

## Parameterliste

### Übergabe
Die Argumentliste bei einem Funktionsaufruf besteht aus *0* bis *n* unbenannten Argumenten, welche über ihre
Reihenfolge in die Parameter übergeben werden. Hierbei ist *n* die Anzahl an Parametern, welche die aufgerufene
Funktion besitzt.

Zudem können noch alle nicht über die Position angegeben Parameter mit einem benannten Argument definiert werden.
Hierbei spielt die Reihenfolge, in welcher die benannten Argumente angegeben werden, keine Rolle.

Alle nicht über Position oder Name angegebenen Parameter müssen einen Default-Wert besitzen, damit jeder Parameter
einen definierten Wert erhält.

```psi
const example = fn(i : int, j : int = 6, k : int = 4);

// Übergabe via Position:
example(1,2,3);

// Übergabe via Name:
example(i: 1, k: 3, j: 2);

// Gemischte Übergabe:
example(1, j: 2, k: 3);

// Default-Übergabe
example(1, 2);    // k = 4
example(1, k: 3); // j = 6
```

### Parameter-Attribute

Parameter können verschiedene Attribute haben, welche das Verhalten verändern. Hierbei gibt es folgende Optionen:

#### `in`
Dies ist das Standardverhalten eines Parameters. Es wird bei der Übergabe des Arguments eine Kopie des Arguments in
die Parametervariable gelegt und der Parameter kann wie eine lokale Variable verwendet werden.

#### `inout`
Ein `inout`-Parameter verhält sich grundlegend anders als ein `in`-Parameter: Es können nur Argumente übergeben werden,
die eine *lvalue* sind, also ein Wert, dem etwas zugewiesen werden kann. Jeder Schreibzugriff auf den Parameter erfolgt
direkt auf den übergebenen *lvalue* und modifiziert diesen.

Der Parameter kann also als Alias für den übergebenen *lvalue* gesehen werden.

```psi
const example = fn(inout i : int)
{
	i *= 2;
};
var x = 10;
example(x);
// x ist jetzt 20
example(x);
// x ist jetzt 40
```

#### `out`
Ein `out`-Parameter verhält sich analog zu einem `inout`-Parameter, nur muss dem `inout`-Parameter zuerst ein Wert
zugewiesen werden, bevor dieser verwendet wird.

Dies bedeutet, dass der ursprüngliche Wert des Arguments verworfen wird und die Funktion den Wert des Arguments mit
einem neuen Wert überschreibt.

```psi
const example = fn(out i : int)
{
	i = 42;
};
var x = 10;
example(x);
// x ist jetzt 42
```

#### `this`
Dieses Attribut kann nur mit dem ersten Parameter in einer Funktion verwendet werden und definiert eine
Erweiterungsmethode.

Diese funktionieren ähnlich wie die Erweiterungsmethoden in C#:

Das erste Argument kann als vorangestellter Ausdruck geschrieben werden und somit den Eindruck eines Methodenaufrufs
erwecken:

```psi
var fun : fn(this i : int); // Erweiterungsmethode
var num : int;

fun(num);  // klassischer aufruf
num.fun(); // erweiterter aufruf
```

### `lazy`
Ein `inout`- oder `out`-Parameter kann als `lazy` markiert werden. Hierbei verändert sich das Verhalten von einem
einfachen Pointer-Verhalten zu einem "read-modify-write"-Verhalten.

Das heißt, dass anstelle einer Referenz auf das Argument zuerst eine Kopie auf den Wert des Arguments übergeben wird,
welche nach erfolgreichem Abschluss der Funktion zurück in das Argument geschrieben wird. Der Vorteil hiervon ist,
dass das übergebene Argument nur einmal zu Abschluss der Funktion verändert wird und nicht wenn die Parametervariable
in der Funktion verändert wird.

## Closures und Captures
In Psi ist jede Funktion ein Closure, welche ihren Erstellungskontext speichert. Das heißt, in einem Funktionswert
wird zusätzlich zum Code der Funktion auch gespeichert, mit welchem Stackframe die Funktion erstellt wurde.

Dies erlaubt den Zugriff auf lokale Variablen des Erstellungskontextes:

```psi
type counttype = fn() -> int;
const makeCounter = fn() -> counttype
{
	var value = 0;
	return fn() -> int
	{
		value += 1;
		return value;
	};
};

var counterA = makeCounter();
var counterB = makeCounter();

print(counterA()); // gibt 1 aus
print(counterA()); // gibt 2 aus
print(counterB()); // gibt 1 aus
print(counterB()); // gibt 2 aus
print(counterA()); // gibt 3 aus

```

Wichtig hierbei ist, dass ein Capture auf einen beliebig weit oben liegenden Erstellungskontext zugreifen kann,
das heißt,Funktionen können sich auch auf den erstellenden Kontext der erstellenden Funktion zugreifen.

Es können alle normalen lokalen Variablen eingefangen werden, nicht aber `out`- oder `inout`-Parameter, da diese eine
Referenz auf einen außerhalb des Erstellungskontext liegenden Wertes darstellen und somit keine kontrollierbare
Lebensdauer haben.