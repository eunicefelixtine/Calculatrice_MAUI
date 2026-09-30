# Calculatrice .NET MAUI

Application de calculatrice scientifique développée en .NET MAUI avec C# et XAML. Le projet vise un rendu moderne, responsive et adapté à Android, avec un moteur de calcul robuste et une interface qui évolue selon la taille de l'écran et l'orientation.

## Fonctionnalités

- Opérations de base : +, −, ×, ÷
- Priorités de calcul et parenthèses
- Fonctions scientifiques : sin, cos, tan, asin, acos, atan, ln, log, log2, exp, abs
- Bascule d'angle : Deg / Rad
- Constantes : π et e
- Racines et puissances : √, ∛, x², x³, xʸ
- Factorielle, inverse, pourcentage contextuel
- Mémoire : MC, MR, M+, M−, MS
- Ans, historique des dernières opérations, copier dans le presse-papiers
- Aperçu en direct du résultat et du calcul en cours
- Gestion de la division par zéro avec message explicite : Impossible
- Correction de précision pour des calculs comme 0,1 + 0,2 = 0,3
- Interface adaptative en portrait et en paysage

## Architecture du projet

- `Calculatrice.cs` : moteur de calcul et analyse des expressions
- `MainPage.xaml` : interface utilisateur en XAML
- `MainPage.xaml.cs` : logique de l'interface, actions des boutons et comportement responsive
- `MauiProgram.cs` : initialisation de l'application MAUI
- `App.xaml`, `AppShell.xaml` : configuration de l'application
- `calculatrice_MAUI.csproj` : configuration du projet MAUI et cibles de build

## Prérequis

- .NET SDK 8
- MAUI workload configuré pour Android
- Android SDK installé et disponible
- Un émulateur Android ou un appareil connecté
- VS 2022 / VS Code avec les outils .NET MAUI

## Démarrage

Depuis la racine du projet :

```bash
dotnet restore
dotnet build -f net8.0-android
```

Pour lancer l'application sur un appareil ou un émulateur :

```bash
dotnet run -f net8.0-android
```

Le projet est configuré pour cibler principalement Android :

```xml
<TargetFrameworks>net8.0-android</TargetFrameworks>
```

## Notes sur l'interface

L'écran a été conçu pour rester lisible et ergonomique sur plusieurs tailles d'affichage :

- affichage principal avec expression et résultat séparés
- panneau scientifique repliable selon l'orientation
- clavier optimisé pour les écrans petits
- gestion dynamique de la police pour éviter les coupures de texte
- historique flottant et navigation simplifiée par le design MAUI

## Moteur de calcul

Le cœur de l'application repose sur un analyseur d'expression. Il transforme la saisie en tokens, applique les priorités des opérateurs, gère les parenthèses, les fonctions mathématiques et les opérations postfixes telles que x², x³ et x!.

## Exemple de calculs supportés

- `2 + 3 * 4 = 14`
- `(2 + 3) * 4 = 20`
- `sin(30)` selon le mode d'angle actif
- `sqrt(9) = 3`
- `10 % = 0,1` selon le contexte

## Auteur

Projet réalisé par NZEUTEM DOMMOE Eunice Felixtine - 22GOO347
