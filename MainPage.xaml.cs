namespace calculatrice_MAUI;

/// 
/// Logique d'interaction du code-behind pour MainPage.xaml
/// 
public partial class MainPage : ContentPage
{
    // Instance de la classe métier contenant toute la logique mathématique
    private readonly Calculatrice _calculatrice = new();
    
    // État de la touche "2nde" pour intervertir les fonctions scientifiques
    private bool _secondeActive;

    // Tableaux des libellés scientifiques (Mode Normal vs Mode 2nde)
    private static readonly string[] SciNorm = { "sin(", "cos(", "tan(", "π", "ln(", "log(", "√", "x²", "xʸ", "(", "x!", "1/x", "x³", ")" };
    private static readonly string[] Sci2nd  = { "asin(", "acos(", "atan(", "e", "log2(", "10^(", "∛", "x³", "xʸ", "(", "exp(", "1/x", "abs(", ")" };

    public MainPage()
    {
        InitializeComponent();

        // Gestion adaptative (Responsive Design) lors des changements de dimensions/orientation
        SizeChanged += (s, e) =>
        {
            bool paysage = Width > Height;
            
            // Ajustement de la taille de police pour éviter que l'affichage déborde
            LblResultat.FontSize = paysage ? 26 : Width < 360 ? 34 : 44;
            LblExpression.FontSize = paysage ? 13 : 15;
            
            // Masquer le pavé scientifique en mode paysage pour maximiser l'espace
            PanneauScience.IsVisible = !paysage;
        };

        Rafraichir();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        Rafraichir();
    }

    /// 
    /// Met à jour les éléments de l'UI à partir des états calculés dans la classe métat Calculatrice
    /// 
    private void Rafraichir()
    {
        LblExpression.Text = _calculatrice.ExpressionAffichage;
        LblResultat.Text = _calculatrice.ResultatAffichage;
        BtnAngle.Text = _calculatrice.Angle == Calculatrice.ModeAngle.Degres ? "Deg" : "Rad";
        LblMemoire.IsVisible = _calculatrice.MemoireActive;
    }

    /// 
    /// Gestionnaire centralisé pour la pression sur un bouton du clavier
    /// 
    private void OnTouche(object? sender, EventArgs e)
    {
        if (sender is not Button btn) return;
        string txt = btn.Text;

        // Routage : Opérateur vs Actions spéciales vs Chiffres
        if (txt.Length == 1 && "+−×÷".Contains(txt[0]))
        {
            _calculatrice.ChoisirOperateur(txt[0]);
        }
        else switch (txt)
        {
            case "C":   _calculatrice.ToutEffacer(); break;
            case "⌫":  _calculatrice.EffacerDernier(); break;
            case "%":   _calculatrice.SaisirPourcentage(); break;
            case "±":   _calculatrice.InverserSigne(); break;
            case "=":   _calculatrice.Egaliser(); break;
            case ",":   _calculatrice.SaisirVirgule(); break;
            case "MC":  _calculatrice.MemoireEffacer(); break;
            case "MR":  _calculatrice.MemoireRappeler(); break;
            case "M+":  _calculatrice.MemoireAjouter(); break;
            case "M−":  _calculatrice.MemoireRetirer(); break;
            case "MS":  _calculatrice.MemoireStocker(); break;
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

    /// 
    /// Alternance entre l'unité d'angle : Degrés / Radians
    /// 
    private void OnBasculeAngle(object? sender, EventArgs e)
    {
        _calculatrice.Angle = _calculatrice.Angle == Calculatrice.ModeAngle.Degres 
            ? Calculatrice.ModeAngle.Radians 
            : Calculatrice.ModeAngle.Degres;
        
        Rafraichir();
    }

    /// 
    /// Modifie dynamiquement les libellés du clavier scientifique (Touches secondaires)
    /// 
    private void OnBasculeSeconde(object? sender, EventArgs e)
    {
        _secondeActive = !_secondeActive;
        string[] libelles = _secondeActive ? Sci2nd : SciNorm;

        // Mise à jour automatique des boutons du pavé scientifique via FindByName
        for (int i = 0; i < libelles.Length; i++)
        {
            if (this.FindByName($"Science{i + 1}") is Button btn)
            {
                btn.Text = libelles[i];
            }
        }
    }

    /// 
    /// Affiche ou masque la fenêtre flottante d'historique
    /// 
    private void OnBasculeHistorique(object? sender, EventArgs e)
    {
        bool etat = !CarteHistorique.IsVisible;
        if (etat) RemplirHistorique();
        
        CarteHistorique.IsVisible = FondHistorique.IsVisible = etat;
        BtnHisto.Text = etat ? "Hist ✕" : "Hist";
    }

    /// 
    /// Génère dynamiquement les boutons représentant l'historique des calculs
    /// 
    private void RemplirHistorique()
    {
        ListeHistorique.Children.Clear();
        var histo = _calculatrice.Historique;

        for (int i = 0; i < histo.Count; i++)
        {
            int idx = i;
            var btn = new Button
            {
                Text = histo[i],
                BackgroundColor = Color.FromArgb("#3A3A3C"),
                TextColor = Colors.White,
                FontSize = 13,
                CornerRadius = 8,
                Padding = new Thickness(8, 6),
                CommandParameter = idx
            };

            // Événement au clic sur un élément de l'historique pour réinjecter le calcul
            btn.Clicked += (s, e) =>
            {
                _calculatrice.RappelerHistorique(idx);
                OnBasculeHistorique(s, e);
                Rafraichir();
            };

            ListeHistorique.Children.Add(btn);
        }
    }

    /// 
    /// Copie le résultat courant directement dans le presse-papier du système mobile
    /// 
    private async void OnCopier(object? sender, EventArgs e) => 
        await Clipboard.Default.SetTextAsync(_calculatrice.ResultatAffichage);
}