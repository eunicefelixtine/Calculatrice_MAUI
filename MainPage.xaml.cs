namespace calculatrice_MAUI;

public partial class MainPage : ContentPage
{
    private readonly Calculatrice _calculatrice = new();

    private static readonly string[] SciencesNormales =
    {
        "sin(", "cos(", "tan(", "π", "ln(", "log(", "√", "x²", "xʸ", "(", "x!", "1/x", "x³", ")",
    };

    private static readonly string[] SciencesDeuxieme =
    {
        "asin(", "acos(", "atan(", "e", "log2(", "10^(", "∛", "x³", "xʸ", "(", "exp(", "1/x", "abs(", ")",
    };

    private bool _secondeActive;
    private bool _paysage;

    public MainPage()
    {
        InitializeComponent();
        SizeChanged += OnTaille;
        Rafraichir();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        Rafraichir();
    }

    private void OnTaille(object? sender, EventArgs e)
    {
        _paysage = Width > Height;

        LblResultat.FontSize = _paysage ? 26 : Width < 360 ? 34 : 44;
        LblExpression.FontSize = _paysage ? 13 : 15;
        PanneauScience.IsVisible = !_paysage;
    }

    private void Rafraichir()
    {
        LblExpression.Text = _calculatrice.ExpressionAffichage;
        LblResultat.Text = _calculatrice.ResultatAffichage;
        BtnAngle.Text = _calculatrice.Angle == Calculatrice.ModeAngle.Degres ? "Deg" : "Rad";
        LblMemoire.IsVisible = _calculatrice.MemoireActive;
    }

    private void OnTouche(object? sender, EventArgs e)
    {
        if (sender is not Button bouton)
        {
            return;
        }

        string texte = bouton.Text;

        if (texte.Length == 1 && texte[0] is '+' or '−' or '×' or '÷')
        {
            _calculatrice.ChoisirOperateur(texte[0]);
        }
        else
        {
            switch (texte)
            {
                case "C":
                    _calculatrice.ToutEffacer();
                    break;
                case "⌫":
                    _calculatrice.EffacerDernier();
                    break;
                case "%":
                    _calculatrice.SaisirPourcentage();
                    break;
                case "±":
                    _calculatrice.InverserSigne();
                    break;
                case "=":
                    _calculatrice.Egaliser();
                    break;
                case ",":
                    _calculatrice.SaisirVirgule();
                    break;
                case "MC":
                    _calculatrice.MemoireEffacer();
                    break;
                case "MR":
                    _calculatrice.MemoireRappeler();
                    break;
                case "M+":
                    _calculatrice.MemoireAjouter();
                    break;
                case "M−":
                    _calculatrice.MemoireRetirer();
                    break;
                case "MS":
                    _calculatrice.MemoireStocker();
                    break;
                case "Ans":
                    _calculatrice.SaisirAns();
                    break;
                default:
                    if (int.TryParse(texte, out int chiffre))
                    {
                        _calculatrice.SaisirChiffre(chiffre);
                    }
                    else
                    {
                        _calculatrice.SaisirSymbole(texte);
                    }

                    break;
            }
        }

        Rafraichir();
    }

    private void OnBasculeAngle(object? sender, EventArgs e)
    {
        _calculatrice.Angle = _calculatrice.Angle == Calculatrice.ModeAngle.Degres
            ? Calculatrice.ModeAngle.Radians
            : Calculatrice.ModeAngle.Degres;
        Rafraichir();
    }

    private void OnBasculeSeconde(object? sender, EventArgs e)
    {
        _secondeActive = !_secondeActive;

        string[] libelles = _secondeActive ? SciencesDeuxieme : SciencesNormales;
        for (int i = 0; i < libelles.Length; i++)
        {
            Button? bouton = nommer(i + 1);
            if (bouton != null)
            {
                bouton.Text = libelles[i];
            }
        }
    }

    private Button? nommer(int index) => index switch
    {
        1 => Science1,
        2 => Science2,
        3 => Science3,
        4 => Science4,
        5 => Science5,
        6 => Science6,
        7 => Science7,
        8 => Science8,
        9 => Science9,
        10 => Science10,
        11 => Science11,
        12 => Science12,
        13 => Science13,
        14 => Science14,
        _ => null,
    };

    private void OnBasculeHistorique(object? sender, EventArgs e)
    {
        if (!CarteHistorique.IsVisible)
        {
            RemplirHistorique();
        }

        bool etat = !CarteHistorique.IsVisible;
        CarteHistorique.IsVisible = etat;
        FondHistorique.IsVisible = etat;
        BtnHisto.Text = etat ? "Hist ✕" : "Hist";
    }

    private void RemplirHistorique()
    {
        ListeHistorique.Children.Clear();

        IReadOnlyList<string> historique = _calculatrice.Historique;
        for (int i = 0; i < historique.Count; i++)
        {
            int index = i;
            var bouton = new Button
            {
                Text = historique[i],
                BackgroundColor = Color.FromArgb("#3A3A3C"),
                TextColor = Colors.White,
                FontSize = 13,
                CornerRadius = 8,
                Padding = new Thickness(8, 6),
                HorizontalOptions = LayoutOptions.Fill,
                CommandParameter = index,
            };
            bouton.Clicked += OnToucheHistorique;
            ListeHistorique.Children.Add(bouton);
        }
    }

    private void OnToucheHistorique(object? sender, EventArgs e)
    {
        if (sender is Button bouton && bouton.CommandParameter is int index)
        {
            _calculatrice.RappelerHistorique(index);
            OnBasculeHistorique(sender, e);
            Rafraichir();
        }
    }

    private async void OnCopier(object? sender, EventArgs e)
    {
        await Clipboard.Default.SetTextAsync(_calculatrice.ResultatAffichage);
    }
}