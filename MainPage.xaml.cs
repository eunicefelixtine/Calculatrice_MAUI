namespace calculatrice_MAUI;

/// <summary>
/// Code-behind de la page principale de la calculatrice MAUI.
/// Il orchestre l'interface utilisateur, les événements des boutons et la synchronisation
/// avec la logique métier contenue dans la classe Calculatrice.
/// </summary>
public partial class MainPage : ContentPage
{
    // Référence unique à la logique de calcul utilisée par toute l'interface.
    private readonly Calculatrice _calculatrice = new();

    // Indique si l'utilisateur a activé le mode 2nde sur le clavier scientifique.
    private bool _secondeActive;

    // Indique si le panneau scientifique est affiché en portrait.
    private bool _scienceVisible;

    // Orientation actuellement appliquée (null = pas encore calculée).
    private bool? _paysageApplique;

    // Taille de police de base du résultat, ajustée selon l'écran puis selon la longueur du texte.
    private double _tailleResultat = 48;

    // Libellés affichés sur les boutons scientifiques selon le mode actif.
    private static readonly string[] SciNorm = { "sin(", "cos(", "tan(", "π", "ln(", "log(", "√", "x²", "xʸ", "(", "x!", "1/x", "x³", ")" };
    private static readonly string[] Sci2nd = { "asin(", "acos(", "atan(", "e", "log2(", "10^(", "∛", "x³", "xʸ", "(", "exp(", "1/x", "abs(", ")" };

    /// <summary>
    /// Initialise la page, le comportement responsive et le rendu initial.
    /// </summary>
    public MainPage()
    {
        InitializeComponent();

        var titre = new Label
        {
            Text = "<Eunice-calculator>",
            FontAttributes = FontAttributes.Bold,
            FontSize = 20,
            TextColor = Colors.White,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
        };

        NavigationPage.SetTitleView(this, titre);
        SizeChanged += OnTailleChangee;
        Rafraichir();
    }

    /// <summary>
    /// S'exécute à chaque affichage de la page pour rafraîchir la vue.
    /// </summary>
    protected override void OnAppearing()
    {
        base.OnAppearing();
        Rafraichir();
    }

    // ------------------------------------------------------------------
    //  Responsive : orientation et taille d'écran
    // ------------------------------------------------------------------

    /// <summary>
    /// Adapte la disposition et les tailles de texte à l'espace disponible.
    /// </summary>
    private void OnTailleChangee(object? sender, EventArgs e)
    {
        if (Width <= 0 || Height <= 0) return;

        bool paysage = Width > Height;
        bool petitEcran = Height < 640 || Width < 360;

        if (_paysageApplique != paysage)
        {
            AppliquerDisposition(paysage);
        }

        // Tailles de police : plus petites en paysage ou sur un petit écran.
        _tailleResultat = paysage ? 32 : petitEcran ? 38 : 48;
        LblExpression.FontSize = paysage || petitEcran ? 13 : 16;

        double policeTouches = paysage || petitEcran ? 19 : 24;
        foreach (Button b in Clavier.Children.OfType<Button>())
        {
            // Les opérateurs et le bouton égal gardent un léger surplus de taille.
            bool special = b.Text is "÷" or "×" or "−" or "+" or "=";
            b.FontSize = special ? policeTouches + 4 : policeTouches;
        }

        // Touches scientifiques : même hauteur que le clavier, police un peu réduite (libellés plus longs).
        foreach (Button b in PanneauScience.Children.OfType<Button>())
        {
            b.FontSize = policeTouches - 5;
        }

        Rafraichir();
    }

    /// <summary>
    /// Portrait : tout est empilé verticalement.
    /// Paysage : infos et science à gauche, clavier à droite.
    /// </summary>
    private void AppliquerDisposition(bool paysage)
    {
        Panneau.RowDefinitions.Clear();
        Panneau.ColumnDefinitions.Clear();

        if (paysage)
        {
            Panneau.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(1, GridUnitType.Star)));
            Panneau.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(1.1, GridUnitType.Star)));

            Panneau.RowDefinitions.Add(new RowDefinition(GridLength.Auto));                         // 0 : outils
            Panneau.RowDefinitions.Add(new RowDefinition(new GridLength(1, GridUnitType.Star)));    // 1 : affichage
            Panneau.RowDefinitions.Add(new RowDefinition(GridLength.Auto));                         // 2 : mémoire
            Panneau.RowDefinitions.Add(new RowDefinition(new GridLength(2.4, GridUnitType.Star)));  // 3 : science

            Placer(BarreOutils, 0, 0, 1);
            Placer(Affichage, 1, 0, 1);
            Placer(LigneMemoire, 2, 0, 1);
            Placer(PanneauScience, 3, 0, 1);
            Placer(Clavier, 0, 1, 4);

            BtnSci.IsVisible = false;
            AbsoluteLayout.SetLayoutBounds(CarteHistorique, new Rect(1, 0.5, 0.5, 0.94));
        }
        else
        {
            Panneau.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(1, GridUnitType.Star)));

            Panneau.RowDefinitions.Add(new RowDefinition(GridLength.Auto));                         // 0 : outils
            Panneau.RowDefinitions.Add(new RowDefinition(new GridLength(1.9, GridUnitType.Star)));  // 1 : affichage
            Panneau.RowDefinitions.Add(new RowDefinition(GridLength.Auto));                         // 2 : mémoire
            Panneau.RowDefinitions.Add(new RowDefinition(new GridLength(0)));                       // 3 : science
            Panneau.RowDefinitions.Add(new RowDefinition(new GridLength(5, GridUnitType.Star)));    // 4 : clavier

            Placer(BarreOutils, 0, 0, 1);
            Placer(Affichage, 1, 0, 1);
            Placer(LigneMemoire, 2, 0, 1);
            Placer(PanneauScience, 3, 0, 1);
            Placer(Clavier, 4, 0, 1);

            BtnSci.IsVisible = true;
            AbsoluteLayout.SetLayoutBounds(CarteHistorique, new Rect(0.5, 1, 0.94, 0.62));
        }

        _paysageApplique = paysage;
        MettreAJourRangeeScience();
    }

    /// <summary>
    /// Affiche ou masque le panneau scientifique et redistribue la hauteur :
    /// quand il apparaît, l'affichage et le clavier rétrécissent proportionnellement.
    /// </summary>
    private void MettreAJourRangeeScience()
    {
        bool paysage = _paysageApplique == true;
        bool visible = paysage || _scienceVisible;

        PanneauScience.IsVisible = visible;

        if (!paysage)
        {
            // On garde un espace d'affichage suffisant pour la valeur finale, même quand
            // le panneau scientifique est visible. Sinon le résultat est comprimé et coupé.
            Panneau.RowDefinitions[1].Height = new GridLength(1.8, GridUnitType.Star);
            Panneau.RowDefinitions[3].Height = visible ? new GridLength(2.4, GridUnitType.Star) : new GridLength(0);
        }
    }

    /// <summary>
    /// Positionne une vue dans la grille principale.
    /// </summary>
    private static void Placer(View vue, int ligne, int colonne, int spanLignes)
    {
        Grid.SetRow(vue, ligne);
        Grid.SetColumn(vue, colonne);
        Grid.SetRowSpan(vue, spanLignes);
    }

    // ------------------------------------------------------------------
    //  Affichage
    // ------------------------------------------------------------------

    /// <summary>
    /// Synchronise les libellés de l'interface avec les données de la calculatrice.
    /// </summary>
    private void Rafraichir()
    {
        string resultat = _calculatrice.ResultatAffichage;

        LblExpression.Text = _calculatrice.ExpressionAffichage;
        LblResultat.Text = resultat;

        // Réduit la police quand le nombre s'allonge pour qu'il reste lisible.
        int n = resultat.Length;
        double facteur = n > 16 ? 0.6 : n > 11 ? 0.75 : n > 8 ? 0.9 : 1.0;
        LblResultat.FontSize = _tailleResultat * facteur;

        BtnAngle.Text = _calculatrice.Angle == Calculatrice.ModeAngle.Degres ? "Deg" : "Rad";
        LblMemoire.IsVisible = _calculatrice.MemoireActive;
    }

    /// <summary>
    /// Point d'entrée centralisé pour gérer les clics sur le clavier.
    /// Il route chaque bouton vers la bonne action métier en fonction de son libellé.
    /// </summary>
    private void OnTouche(object? sender, EventArgs e)
    {
        if (sender is not Button btn) return;
        string txt = btn.Text;

        if (txt.Length == 1 && "+−×÷".Contains(txt[0]))
        {
            _calculatrice.ChoisirOperateur(txt[0]);
        }
        else switch (txt)
        {
            case "C": _calculatrice.ToutEffacer(); break;
            case "⌫": _calculatrice.EffacerDernier(); break;
            case "%": _calculatrice.SaisirPourcentage(); break;
            case "±": _calculatrice.InverserSigne(); break;
            case "=": _calculatrice.Egaliser(); break;
            case ",": _calculatrice.SaisirVirgule(); break;
            case "MC": _calculatrice.MemoireEffacer(); break;
            case "MR": _calculatrice.MemoireRappeler(); break;
            case "M+": _calculatrice.MemoireAjouter(); break;
            case "M−": _calculatrice.MemoireRetirer(); break;
            case "MS": _calculatrice.MemoireStocker(); break;
            case "Ans": _calculatrice.SaisirAns(); break;
            default:
                if (int.TryParse(txt, out int val))
                    _calculatrice.SaisirChiffre(val);
                else
                    _calculatrice.SaisirSymbole(txt);
                break;
        }

        Rafraichir();
    }

    /// <summary>
    /// Bascule entre degrés et radians pour le calcul des fonctions trigonométriques.
    /// </summary>
    private void OnBasculeAngle(object? sender, EventArgs e)
    {
        _calculatrice.Angle = _calculatrice.Angle == Calculatrice.ModeAngle.Degres
            ? Calculatrice.ModeAngle.Radians
            : Calculatrice.ModeAngle.Degres;

        Rafraichir();
    }

    /// <summary>
    /// Affiche ou masque le panneau scientifique (portrait uniquement).
    /// </summary>
    private void OnBasculeScience(object? sender, EventArgs e)
    {
        _scienceVisible = !_scienceVisible;
        MettreAJourRangeeScience();

        // Le bouton change de couleur quand le panneau est ouvert.
        BtnSci.TextColor = (Color)Resources[_scienceVisible ? "Accent" : "Texte"];
    }

    /// <summary>
    /// Active ou désactive le mode 2nde sur les fonctions scientifiques.
    /// </summary>
    private void OnBasculeSeconde(object? sender, EventArgs e)
    {
        _secondeActive = !_secondeActive;
        string[] libelles = _secondeActive ? Sci2nd : SciNorm;

        // Met à jour chaque bouton scientifique en fonction du mode actuel.
        for (int i = 0; i < libelles.Length; i++)
        {
            if (this.FindByName($"Science{i + 1}") is Button btn)
            {
                btn.Text = libelles[i];
            }
        }
    }

    /// <summary>
    /// Affiche ou masque le panneau flottant de l'historique des calculs.
    /// </summary>
    private void OnBasculeHistorique(object? sender, EventArgs e)
    {
        bool etat = !CarteHistorique.IsVisible;
        if (etat) RemplirHistorique();

        CarteHistorique.IsVisible = FondHistorique.IsVisible = etat;
        BtnHisto.Text = etat ? "Hist ✕" : "Hist";
    }

    /// <summary>
    /// Efface complètement l'historique des calculs.
    /// </summary>
    private void OnViderHistorique(object? sender, EventArgs e)
    {
        _calculatrice.ViderHistorique();
        RemplirHistorique();
    }

    /// <summary>
    /// Reconstruit la liste des calculs déjà effectués dans le panneau d'historique.
    /// </summary>
    private void RemplirHistorique()
    {
        ListeHistorique.Children.Clear();
        var histo = _calculatrice.Historique;

        if (histo.Count == 0)
        {
            ListeHistorique.Children.Add(new Label
            {
                Text = "Aucun calcul pour le moment",
                TextColor = (Color)Resources["TexteDoux"],
                FontSize = 14,
                HorizontalTextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 20)
            });
            return;
        }

        for (int i = 0; i < histo.Count; i++)
        {
            int idx = i;
            var btn = new Button
            {
                Text = histo[i],
                BackgroundColor = (Color)Resources["SurfaceHaute"],
                TextColor = (Color)Resources["Texte"],
                FontSize = 14,
                CornerRadius = 12,
                Padding = new Thickness(12, 10),
                HorizontalOptions = LayoutOptions.Fill,
                CommandParameter = idx
            };

            // Le clic recharge un calcul précédent dans l'écran principal.
            btn.Clicked += (s, e) =>
            {
                _calculatrice.RappelerHistorique(idx);
                OnBasculeHistorique(s, e);
                Rafraichir();
            };

            ListeHistorique.Children.Add(btn);
        }
    }

    /// <summary>
    /// Copie le résultat actuel dans le presse-papiers du système.
    /// </summary>
    private async void OnCopier(object? sender, EventArgs e) =>
        await Clipboard.Default.SetTextAsync(_calculatrice.ResultatAffichage);
}