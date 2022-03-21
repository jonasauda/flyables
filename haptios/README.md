# Einleitung

HaptiOS ist eine ASP.NET Core Anwendung. Sie besteht aus der Serveranwendung im
Unterordner `HaptiOS.Src` und dem dazugehörigen Testprojekt `HaptiOS.Test`.
Beide Projekte sind in einer Solution zur einfachen Verwendung in z.B. Visual
Studio organisiert. Für weiter reichende Dokumentation und den Umgang mit Razor
Pages bietet Microsoft eine Erste Schritte Anleitung. Das erforderliche Projekt
ist bereits erstellt und kann geöffnet werden. In der Doku kann mit dem Punkt
[Projektdateien und
-ordner](https://docs.microsoft.com/de-de/aspnet/core/tutorials/razor-pages/razor-pages-start?view=aspnetcore-2.1#project-files-and-folders)
begonnen werden.

# Requirements

In den erforderlichen Komponenten der Dokumentation ist Visual Studio Enterprise
angegeben, welches aber nur aus Editierungsgründen und dem eins zu eins
Nachvollziehen der Dokumentation gelistet ist. Es genügt eine Installation von
[.NET Core 2.1 SDK oder höher](https://www.microsoft.com/net/download) und ein
Editor (Sublime Text, Atom, Visual Studio Code, Vim, Emacs...). Unter der
Verwendung von Omnisharp kann eine komfortable Editierung möglich sein. Unter
Linux sind leider Probleme aufgetaucht, da Abhängigkeiten von Komponenten von
Komponenten nicht geladen werden konnten.

# Installation

Wenn .NET Core installiert ist über die Kommandozeile der Befehl `dotnet`
verfügbar. Über diesen kann die Webanwendung gestartet werden.

```powershell
cd <anwendungsverzeichnis>
dotnet run
```

# Konfiguration

Da HaptiOS eine Vielzahl an Netzwerkverbindungen verwendet werden die Parameter
zur Kommunikation in der Datei `haptios.config.json` notiert. Diese werden
beim Start des Programms geladen. HaptiOS empfängt Daten von einer laufenden
VinteR Instanz und einer Unity-Anwendung. Diese Daten werden dazu verwendet
Dronen und Blimps korrekt positionieren zu können. Für den Einsatz von VinteR
wird lediglich folgende Einstellung benötigt:

```json
"vinter": {
    "ip": "127.0.0.1",
    "local.port": 6040
}
```

Die IP zeigt auf den Rechner auf dem VinteR läuft. Der Port ist der von HaptiOS
verwendete Empfängerport.

Für die Unity-Anwendung genügt ein Empfängerport nicht mehr aus.

```json
"unity": {
    "ip": "127.0.0.1",
    "udp.local.ports": {
        "drone.control": 6042,
        "blimp.control": 6043
    }
}
```

Es ist sinnvoll Daten für die Dronen und die Blimps über separate Ports zu
versenden, da innerhalb von HaptiOS weniger bis keine Selektion erfolgen muss,
auf welches Objekt sich das empfangene Paket bezieht.

Nachdem die Daten verarbeitet wurden können Sie an den für das Gerät
entsprechenden Adressaten weitergegeben werden.

```json
"flight.controller": {
    "drone": {
        "ip": "132.252.211.84",
        "remote.port": 4242,
        "local.port": 5210
    },
    "blimp": {
        "ip": "132.252.211.84",
        "remote.port": 4250,
        "local.port": 5211
    } 
}
```

Höchstwahrscheinlich ist die IP für Dronen- und Blimpsteuerung gleich, kann aber
wenn notwendig separat aufgeführt werden und bezeichnet die IP, auf der die
konkrete Steuerungseinheit von Drone/Blimp läuft. Der `remote.port` ist der Port
auf dem die Steuerungseinheit Daten empfängt. Der `local.port` dient dazu
Antworten an HaptiOS zu übertragen.

Damit keine Probleme von lokalen Einstellungen innerhalb der Versionsverwaltung
entstehen kann in der Datei `haptios.config.Development.json` jeder Wert
definiert werden. Dadurch werden die Einstellungen zur Laufzeit des Programms
überschrieben.


# Reproduzierung

Sollte eine Solution mit derselben Struktur erzeugt werden müssen folgende
Schritte nachvollzogen werden.

```powershell
mkdir Solution
cd Solution
dotnet new sln
```

`dotnet new sln` erzeugt lediglich eine neue Projektmappe.

```powershell
dotnet new razor -o Application.Src
```

Es werden die erforderlichen Komponenten für die Erstellung einer Webanwendung
geladen, worauf diese in `Application.Src` verfügbar ist.

```powershell
dotnet new xunit -o Application.Test
cd Application.Test
dotnet add reference ../Application.Src/Application.Src.csproj
```

Ein neues xUnit-Testprojekt wird erstellt und eine Abhängigkeit zum Quellprojekt
hinzugefügt.

```powershell
cd ..
dotnet sln add Application.Src/Application.Src.csproj
dotnet sln add Application.Test/Application.Test.csproj
```

Zuletzt werden die zwei erstellten Projekt der Mappe hinzugefügt.