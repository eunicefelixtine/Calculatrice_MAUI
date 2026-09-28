# Calculatrice .NET MAUI

Calculatrice scientifique pour Android développée en **.NET MAUI** (C# / XAML).

## Fonctionnalités

- Opérations de base : `+`, `−`, `×`, `÷`, `^`
- Priorités et parenthèses : `2 + 3 × 4 = 14`, `(2 + 3) × 4 = 20`
- Fonctions scientifiques : `sin`, `cos`, `tan`, `asin`, `acos`, `atan`, `ln`, `log`, `log2`, `exp`, `abs`
- Bascule d'angle **Deg / Rad**
- Constantes `π` et `e`, racines `√` et `∛`, puissances `x²`, `x³`, `xʸ`, factorielle `x!`, inverse `1/x`
- Pourcentage contextuel : `200 + 10 % = 220`, `50 % = 0,5`
- Mémoire : `MC`, `MR`, `M+`, `M−`, `MS` avec indicateur `M`
- **Ans** (reprend le dernier résultat), **historique** des 20 dernières opérations, **Copier** (presse-papiers)
- Aperçu **en direct** du résultat pendant la saisie ; l'opération est affichée **au-dessus** du résultat
- Division par zéro → `Impossible` (aucune plante, aucun débordement)
- `0,1 + 0,2 = 0,3` : la virgule est gérée sans erreur d'arrondi perceptible

## Types de layout utilisés (7, dans une seule page)

| Layout | Rôle |
| --- | --- |
| `AbsoluteLayout` | Racine de la page : positionne le panneau principal et la carte d'historique flottante |
| `Grid` | Structure générale de la page et pavé numérique 4×5 |
| `Border` | Cadre de la zone d'affichage et de la carte d'historique |
| `VerticalStackLayout` | Empile l'opération au-dessus du résultat dans l'affichage |
| `ScrollView` | Défilement horizontal de l'opération (jamais coupée) + liste d'historique |
| `HorizontalStackLayout` | Rangées de puces (Deg/Ans/Hist/Copier) et boutons de mémoire (MC…MS) |
| `FlexLayout` | Clavier scientifique 15 touches (enveloppement sur 3 rangées) |

L'interface s'adapte à l'orientation et aux petites tailles d'écran : le panneau
scientifique est replié en mode paysage pour laisser le pavé numérique utilisable,
et les polices sont réduites si l'écran est étroit.

## Moteur de calcul

`Calculatrice.cs` implémente un **analyseur d'expression** (transformation de
Shunting-Yard) : tokenisation, priorités des opérateurs, parenthèses
auto-fermées, signes unaires, fonctions, postfixes (`²`, `³`, `!`). Le harnais de
tests (`Program.cs` hors dépôt) couvre 77 scénarios.

## Compilation

```bash
export TMPDIR="$HOME/.cache/dotnet-tmp"
dotnet build -f net10.0-android
```

APK signé généré : `bin/Debug/net10.0-android/com.epi.calculatrice-Signed.apk`

## Structure

- `Calculatrice.cs` — moteur de calcul (expression)
- `MainPage.xaml` + `MainPage.xaml.cs` — interface (7 layouts)
- `MauiProgram.cs`, `App.xaml`, `AppShell.xaml` — amorçage de l'application