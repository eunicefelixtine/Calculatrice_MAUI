using System.Globalization;

namespace calculatrice_MAUI;

/// <summary>
/// Classe métier de la calculatrice.
/// Elle contient l'état courant de l'expression, la gestion de la mémoire,
/// l'historique et les opérations de calcul ainsi que l'évaluation des expressions.
/// </summary>
public sealed class Calculatrice
{
    public enum ModeAngle { Degres, Radians }

    // Caractères visuels utilisés dans l'interface pour les opérateurs.
    private const char Moins = '−';
    private const char Multiplier = '×';
    private const char Diviser = '÷';

    // Limites de sécurité pour éviter un affichage ou un historique trop volumineux.
    private const int LongueurMax = 40;
    private const int HistoriqueMax = 20;

    // État interne de la calculatrice.
    private string _texte = string.Empty;
    private bool _apresEgal;
    private bool _erreur;
    private double _dernierResultat;
    private string _derniereExpression = string.Empty;
    private double? _memoire;
    private ModeAngle _mode = ModeAngle.Degres;
    private readonly List<string> _historique = new();

    /// <summary>
    /// Unité d'angle active pour les opérations trigonométriques.
    /// </summary>
    public ModeAngle Angle
    {
        get => _mode;
        set => _mode = value;
    }

    /// <summary>
    /// Contenu textuel de l'expression actuellement construite.
    /// </summary>
    public string Texte => _texte;

    /// <summary>
    /// Indique si une valeur est actuellement stockée en mémoire.
    /// </summary>
    public bool MemoireActive => _memoire.HasValue;

    /// <summary>
    /// Indique si la calculatrice est dans un état d'erreur.
    /// </summary>
    public bool EstErreur => _erreur;

    /// <summary>
    /// Historique des opérations effectuées, limité à un nombre défini.
    /// </summary>
    public IReadOnlyList<string> Historique => _historique;

    /// <summary>
    /// Vide complètement l'historique enregistré.
    /// </summary>
    public void ViderHistorique()
    {
        _historique.Clear();
    }

    /// <summary>
    /// Texte affiché dans la zone d'expression de l'interface.
    /// </summary>
    public string ExpressionAffichage =>
        _erreur ? string.Empty
        : _apresEgal ? string.Empty
        : _texte.Length == 0 ? "0"
        : _texte;

    public string ResultatAffichage
    {
        get
        {
            if (_erreur)
            {
                return "Impossible";
            }

            if (_apresEgal)
            {
                return _texte;
            }

            if (_texte.Length == 0)
            {
                return "0";
            }

            double? valeur = Evaluer(_texte);
            if (valeur.HasValue)
            {
                return double.IsFinite(valeur.Value) ? Formater(valeur.Value) : "Impossible";
            }

            string dernier = DernierOperandeAffichable();
            return dernier.Length > 0 ? dernier : "0";
        }
    }

    public void SaisirChiffre(int chiffre)
    {
        if (_erreur || chiffre < 0 || chiffre > 9)
        {
            return;
        }

        Repartir();
        string chiffreTexte = chiffre.ToString(CultureInfo.InvariantCulture);

        if (LancementNouveau())
        {
            _texte = _texte == "0" ? chiffreTexte : _texte + chiffreTexte;
        }
        else if (_texte == "0" && _texte.Length == 1)
        {
            _texte = chiffreTexte;
        }
        else
        {
            _texte += chiffreTexte;
        }

        LimiterLongueur();
    }

    public void SaisirVirgule()
    {
        if (_erreur)
        {
            return;
        }

        Repartir();

        if (_texte.Length == 0)
        {
            _texte = "0,";
            LimiterLongueur();
            return;
        }

        char dernier = _texte[^1];

        if (dernier == ',')
        {
            return;
        }

        _texte += EstExtremiteOperande(dernier) ? "," : "0,";
        LimiterLongueur();
    }

    public void ChoisirOperateur(char operateur)
    {
        if (_erreur)
        {
            return;
        }

        char op = operateur switch
        {
            '+' => '+',
            '-' or Moins => Moins,
            '*' or Multiplier => Multiplier,
            '/' or Diviser => Diviser,
            '^' => '^',
            _ => '\0',
        };

        if (op == '\0')
        {
            return;
        }

        if (_apresEgal)
        {
            _texte = Formater(_dernierResultat);
            _apresEgal = false;
        }

        if (_texte.Length == 0)
        {
            if (op == Moins)
            {
                _texte = Moins.ToString();
            }

            return;
        }

        char dernier = _texte[^1];

        if (EstExtremiteOperande(dernier))
        {
            _texte += op;
        }
        else if (dernier == Moins)
        {
            _texte = _texte[..^1] + op;
        }
        else if (op == Moins)
        {
            _texte += Moins;
        }

        LimiterLongueur();
    }

    public void SaisirSymbole(string symbole)
    {
        if (_erreur)
        {
            return;
        }

        if (symbole == "(")
        {
            Repartir();
            if (_texte.Length > 0 && EstExtremiteOperande(_texte[^1]))
            {
                _texte += Multiplier;
            }

            _texte += "(";
            LimiterLongueur();
            return;
        }

        if (symbole == ")")
        {
            Repartir();
            if (EquilibreParentheses(_texte) > 0 && _texte.Length > 0 && EstExtremiteOperande(_texte[^1]))
            {
                _texte += ")";
                LimiterLongueur();
            }

            return;
        }

        string s = SymboleInterne(symbole);
        if (s.Length == 0)
        {
            return;
        }

        ReprendreResultat();

        if (_texte.Length > 0 && EstExtremiteOperande(_texte[^1]) && CommenceParOperande(s))
        {
            _texte += Multiplier;
        }

        _texte += s;
        LimiterLongueur();
    }

    public void SaisirPourcentage()
    {
        if (_erreur || _texte.Length == 0)
        {
            return;
        }

        ReprendreResultat();
        int debut = DebutDernierOperande(_texte);
        string droite = _texte[debut..];
        if (!double.TryParse(droite.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out double valeur))
        {
            return;
        }

        string avant = _texte[..debut];
        double resultat = valeur / 100d;

        if (avant.Length > 0)
        {
            char op = avant[^1];
            if (op == '+' || op == Moins)
            {
                string gaucheTexte = avant[..^1];
                double? gauche = Evaluer(gaucheTexte);
                if (gauche.HasValue && double.IsFinite(gauche.Value))
                {
                    resultat = gauche.Value * valeur / 100d;
                }
            }
        }

        _texte = avant + Formater(resultat);
        LimiterLongueur();
    }

    public void InverserSigne()
    {
        if (_erreur || _texte.Length == 0)
        {
            return;
        }

        ReprendreResultat();
        int debut = DebutDernierOperande(_texte);

        if (debut > 0 && _texte[debut - 1] == Moins)
        {
            _texte = _texte.Remove(debut - 1, 1);
            return;
        }

        _texte = _texte.Insert(debut, Moins.ToString());
        LimiterLongueur();
    }

    public void SaisirAns()
    {
        if (_erreur || !_apresEgal)
        {
            return;
        }

        _texte = _texte + Multiplier + Formater(_dernierResultat);
        _apresEgal = false;
        LimiterLongueur();
    }

    public void Egaliser()
    {
        if (_erreur)
        {
            return;
        }

        string finale = _texte;

        while (finale.Length > 0)
        {
            char dernier = finale[^1];
            if (dernier is '+' or Moins or Multiplier or Diviser or '^')
            {
                finale = finale[..^1];
                continue;
            }

            break;
        }

        int ouverts = EquilibreParentheses(finale);
        if (ouverts > 0)
        {
            finale += new string(')', ouverts);
        }
        else if (ouverts < 0)
        {
            _erreur = true;
            return;
        }

        if (finale.Length == 0)
        {
            return;
        }

        double? valeur = Evaluer(finale);
        if (!valeur.HasValue || !double.IsFinite(valeur.Value))
        {
            _erreur = true;
            return;
        }

        _dernierResultat = valeur.Value;
        _apresEgal = true;
        _derniereExpression = finale;
        _texte = Formater(valeur.Value);

        _historique.Add(finale + " = " + _texte);
        if (_historique.Count > HistoriqueMax)
        {
            _historique.RemoveAt(0);
        }
    }

    public void EffacerDernier()
    {
        if (_erreur || _texte.Length == 0)
        {
            return;
        }

        if (_texte[^1] == '(')
        {
            int debut = _texte.Length - 1;
            while (debut > 0 && char.IsLetter(_texte[debut - 1]))
            {
                debut--;
            }

            _texte = _texte[..debut];
            return;
        }

        _texte = _texte[..^1];
    }

    public void ToutEffacer()
    {
        _texte = string.Empty;
        _apresEgal = false;
        _erreur = false;
        _dernierResultat = 0d;
        _derniereExpression = string.Empty;
    }

    public void MemoireEffacer()
    {
        _memoire = null;
    }

    public void MemoireStocker()
    {
        double? valeur = ValeurCourante();
        if (valeur.HasValue && double.IsFinite(valeur.Value))
        {
            _memoire = valeur.Value;
        }
    }

    public void MemoireAjouter()
    {
        double? valeur = ValeurCourante();
        if (valeur.HasValue && double.IsFinite(valeur.Value))
        {
            _memoire = (_memoire ?? 0d) + valeur.Value;
        }
    }

    public void MemoireRetirer()
    {
        double? valeur = ValeurCourante();
        if (valeur.HasValue && double.IsFinite(valeur.Value))
        {
            _memoire = (_memoire ?? 0d) - valeur.Value;
        }
    }

    public void MemoireRappeler()
    {
        if (!_memoire.HasValue)
        {
            return;
        }

        ReprendreResultat();
        if (_texte.Length > 0 && EstExtremiteOperande(_texte[^1]))
        {
            _texte += Multiplier;
        }

        _texte += Formater(_memoire.Value);
        LimiterLongueur();
    }

    public void RappelerHistorique(int index)
    {
        if (index < 0 || index >= _historique.Count)
        {
            return;
        }

        string line = _historique[index];
        int sep = line.IndexOf(" = ", StringComparison.Ordinal);
        if (sep < 0)
        {
            return;
        }

        _texte = line[(sep + 3)..];
        _apresEgal = true;
    }

    private void Repartir()
    {
        if (_apresEgal)
        {
            _texte = string.Empty;
            _apresEgal = false;
        }
    }

    private void ReprendreResultat()
    {
        if (_apresEgal)
        {
            _texte = Formater(_dernierResultat);
            _apresEgal = false;
        }
    }

    private bool LancementNouveau() => _texte.Length == 0;

    private void LimiterLongueur()
    {
        if (_texte.Length > LongueurMax)
        {
            _texte = _texte[..LongueurMax];
        }
    }

    private double? ValeurCourante()
    {
        if (_apresEgal)
        {
            return _dernierResultat;
        }

        if (_texte.Length == 0)
        {
            return null;
        }

        double? valeur = Evaluer(_texte);
        if (valeur.HasValue && !double.IsFinite(valeur.Value))
        {
            return null;
        }

        if (valeur.HasValue)
        {
            return valeur.Value;
        }

        string dernier = DernierOperandeAffichable();
        return dernier.Length > 0 ? double.Parse(dernier.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture) : null;
    }

    private string DernierOperandeAffichable()
    {
        int debut = DebutDernierOperande(_texte);
        string dernier = _texte[debut..].TrimEnd(',');
        if (double.TryParse(dernier.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out _))
        {
            return dernier;
        }

        return string.Empty;
    }

    private static int DebutDernierOperande(string texte)
    {
        int i = texte.Length - 1;
        while (i >= 0 && (char.IsAsciiDigit(texte[i]) || texte[i] == ','))
        {
            i--;
        }

        return i + 1;
    }

    private static int EquilibreParentheses(string texte)
    {
        int balance = 0;
        foreach (char c in texte)
        {
            if (c == '(')
            {
                balance++;
            }
            else if (c == ')')
            {
                balance--;
            }
        }

        return balance;
    }

    private static bool EstExtremiteOperande(char c) =>
        char.IsAsciiDigit(c) || c == ',' || c == ')' || c is '!' or '²' or '³' or 'π';

    private static bool CommenceParOperande(string s)
    {
        char premier = s[0];
        return char.IsAsciiDigit(premier) || premier is 'π' or 'e' or '√' or '∛' or '(';
    }

    private static string SymboleInterne(string symbole) => symbole switch
    {
        "π" => "π",
        "e" => "e",
        "√" => "√",
        "∛" => "∛",
        "x²" => "²",
        "x³" => "³",
        "xʸ" or "^" => "^",
        "1/x" => "^-1",
        "x!" => "!",
        "sin(" => "sin(",
        "cos(" => "cos(",
        "tan(" => "tan(",
        "asin(" => "asin(",
        "acos(" => "acos(",
        "atan(" => "atan(",
        "ln(" => "ln(",
        "log(" => "log(",
        "log2(" => "log2(",
        "10^(" => "10^(",
        "exp(" => "exp(",
        "abs(" => "abs(",
        _ => string.Empty,
    };

    private double? Evaluer(string texte)
    {
        var jetons = Tokeniser(texte);
        if (jetons == null)
        {
            return null;
        }

        var rpn = ShuntingYard(jetons);
        if (rpn == null)
        {
            return null;
        }

        var tas = new Stack<double>();
        foreach (Jeton j in rpn)
        {
            switch (j.Kind)
            {
                case KindJeton.Nombre:
                    tas.Push(j.Valeur);
                    break;

                case KindJeton.Binaire:
                    if (tas.Count < 2)
                    {
                        return null;
                    }

                    double droite = tas.Pop();
                    double gauche = tas.Pop();
                    double r = Calculer(gauche, droite, j.Caractere);
                    if (double.IsNaN(r))
                    {
                        return double.NaN;
                    }

                    tas.Push(r);
                    break;

                case KindJeton.Unaire:
                    if (tas.Count < 1)
                    {
                        return null;
                    }

                    double u = tas.Pop();
                    double vu = j.Caractere switch
                    {
                        Moins => -u,
                        '√' => Math.Sqrt(u),
                        '∛' => Math.Cbrt(u),
                        _ => double.NaN,
                    };
                    tas.Push(vu);
                    break;

                case KindJeton.Postfixe:
                    if (tas.Count < 1)
                    {
                        return null;
                    }

                    double p = tas.Pop();
                    double vp = j.Caractere switch
                    {
                        '²' => p * p,
                        '³' => p * p * p,
                        '!' => Factorielle(p),
                        _ => double.NaN,
                    };
                    tas.Push(vp);
                    break;

                case KindJeton.Fonction:
                    if (tas.Count < 1)
                    {
                        return null;
                    }

                    double argument = tas.Pop();
                    tas.Push(AppliqueFonction(j.Nom, argument));
                    break;
            }
        }

        if (tas.Count != 1)
        {
            return null;
        }

        return tas.Pop();
    }

    private double AppliqueFonction(string nom, double x)
    {
        switch (nom)
        {
            case "sin":
            case "cos":
            case "tan":
                {
                    double radians = _mode == ModeAngle.Radians ? x : x * Math.PI / 180d;
                    return nom switch
                    {
                        "sin" => Math.Sin(radians),
                        "cos" => Math.Cos(radians),
                        _ => Math.Tan(radians),
                    };
                }

            case "asin":
            case "acos":
            case "atan":
                {
                    double radians = nom switch
                    {
                        "asin" => Math.Asin(x),
                        "acos" => Math.Acos(x),
                        _ => Math.Atan(x),
                    };
                    return _mode == ModeAngle.Radians ? radians : radians * 180d / Math.PI;
                }

            case "ln":
                return Math.Log(x);
            case "log":
                return Math.Log10(x);
            case "log2":
                return Math.Log2(x);
            case "exp":
                return Math.Exp(x);
            case "abs":
                return Math.Abs(x);
            default:
                return double.NaN;
        }
    }

    private static double Calculer(double gauche, double droite, char operateur) =>
        operateur switch
        {
            '+' => gauche + droite,
            Moins => gauche - droite,
            Multiplier => gauche * droite,
            Diviser => droite == 0d ? double.NaN : gauche / droite,
            '^' => Math.Pow(gauche, droite),
            _ => double.NaN,
        };

    private static double Factorielle(double x)
    {
        if (x < 0d || x > 170d || x != Math.Floor(x))
        {
            return double.NaN;
        }

        double resultat = 1d;
        for (int i = 2; i <= (int)x; i++)
        {
            resultat *= i;
        }

        return resultat;
    }

    private enum KindJeton
    {
        Nombre,
        Binaire,
        Unaire,
        Postfixe,
        Fonction,
        Parenthese,
    }

    private readonly record struct Jeton(KindJeton Kind, double Valeur, char Caractere, string Nom);

    private static readonly string[] FonctionsReconnues =
        { "sin", "cos", "tan", "asin", "acos", "atan", "ln", "log", "log2", "exp", "abs" };

    private static List<Jeton>? Tokeniser(string texte)
    {
        var jetons = new List<Jeton>();
        int i = 0;
        int n = texte.Length;

        while (i < n)
        {
            char c = texte[i];

            if (char.IsAsciiDigit(c) || c == '.' || c == ',')
            {
                int debut = i;
                while (i < n && (char.IsAsciiDigit(texte[i]) || texte[i] == '.' || texte[i] == ','))
                {
                    i++;
                }

                string numero = texte[debut..i];
                int virgules = numero.Count(ch => ch is '.' or ',');
                if (virgules > 1 || !double.TryParse(numero.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out double valeur))
                {
                    return null;
                }

                jetons.Add(new Jeton(KindJeton.Nombre, valeur, '\0', string.Empty));
            }
            else if (c is '+' or '×' or '÷' or '^' or Moins or '-')
            {
                jetons.Add(new Jeton(KindJeton.Binaire, 0d, c == '-' ? Moins : c, string.Empty));
                i++;
            }
            else if (c == '(')
            {
                jetons.Add(new Jeton(KindJeton.Parenthese, 0d, '(', string.Empty));
                i++;
            }
            else if (c == ')')
            {
                jetons.Add(new Jeton(KindJeton.Parenthese, 0d, ')', string.Empty));
                i++;
            }
            else if (c is 'π' or '√' or '∛' or '²' or '³' or '!')
            {
                if (c == 'π')
                {
                    jetons.Add(new Jeton(KindJeton.Nombre, Math.PI, '\0', string.Empty));
                }
                else if (c == '√' || c == '∛')
                {
                    jetons.Add(new Jeton(KindJeton.Unaire, 0d, c, string.Empty));
                }
                else
                {
                    jetons.Add(new Jeton(KindJeton.Postfixe, 0d, c, string.Empty));
                }

                i++;
            }
            else if (char.IsLetter(c))
            {
                int debut = i;
                while (i < n && char.IsLetterOrDigit(texte[i]) && texte[i] != '(')
                {
                    i++;
                }

                string nom = texte[debut..i];

                if (i < n && texte[i] == '(' && FonctionsReconnues.Contains(nom))
                {
                    jetons.Add(new Jeton(KindJeton.Fonction, 0d, '\0', nom));
                    i++;
                    jetons.Add(new Jeton(KindJeton.Parenthese, 0d, '(', string.Empty));
                }
                else if (nom == "e")
                {
                    jetons.Add(new Jeton(KindJeton.Nombre, Math.E, '\0', string.Empty));
                }
                else
                {
                    return null;
                }
            }
            else
            {
                return null;
            }
        }

        return jetons;
    }

    private static int Precedence(char operateur) => operateur switch
    {
        '+' or Moins => 2,
        Multiplier or Diviser => 3,
        '^' => 4,
        '√' or '∛' => 4,
        _ => 0,
    };

    private static bool AssociatifGauche(char operateur) => operateur != '^';

    private static List<Jeton>? ShuntingYard(List<Jeton> jetons)
    {
        var sortie = new List<Jeton>(jetons.Count);
        var pile = new Stack<Jeton>();

        for (int i = 0; i < jetons.Count; i++)
        {
            Jeton j = jetons[i];
            Jeton precedent = i > 0 ? jetons[i - 1] : default;

            switch (j.Kind)
            {
                case KindJeton.Nombre:
                case KindJeton.Postfixe:
                    sortie.Add(j);
                    break;

                case KindJeton.Unaire:
                case KindJeton.Fonction:
                    pile.Push(j);
                    break;

                case KindJeton.Parenthese when j.Caractere == '(':
                    pile.Push(j);
                    break;

                case KindJeton.Parenthese:
                    {
                        bool trouve = false;
                        while (pile.Count > 0)
                        {
                            Jeton sommet = pile.Pop();
                            if (sommet.Kind == KindJeton.Parenthese && sommet.Caractere == '(')
                            {
                                trouve = true;
                                break;
                            }

                            sortie.Add(sommet);
                        }

                        if (!trouve)
                        {
                            return null;
                        }

                        if (pile.Count > 0 && pile.Peek().Kind == KindJeton.Fonction)
                        {
                            sortie.Add(pile.Pop());
                        }

                        break;
                    }

                case KindJeton.Binaire:
                    {
                        char op = j.Caractere;
                        bool estUnaire = i == 0
                            || precedent.Kind is KindJeton.Binaire or KindJeton.Unaire or KindJeton.Fonction
                            || (precedent.Kind == KindJeton.Parenthese && precedent.Caractere == '(');

                        if (estUnaire)
                        {
                            pile.Push(new Jeton(KindJeton.Unaire, 0d, op, string.Empty));
                            break;
                        }

                        while (pile.Count > 0)
                        {
                            Jeton sommet = pile.Peek();
                            if (sommet.Kind == KindJeton.Binaire)
                            {
                                int sp = Precedence(sommet.Caractere);
                                int ip = Precedence(op);
                                if (sp > ip || (sp == ip && AssociatifGauche(op)))
                                {
                                    sortie.Add(pile.Pop());
                                    continue;
                                }

                                break;
                            }

                            if (sommet.Kind == KindJeton.Unaire && Precedence(sommet.Caractere) >= Precedence(op))
                            {
                                sortie.Add(pile.Pop());
                                continue;
                            }

                            break;
                        }

                        pile.Push(j);
                        break;
                    }
            }
        }

        while (pile.Count > 0)
        {
            Jeton sommet = pile.Pop();
            if (sommet.Kind == KindJeton.Parenthese)
            {
                return null;
            }

            sortie.Add(sommet);
        }

        return sortie;
    }

    public static string Formater(double valeur)
    {
        if (!double.IsFinite(valeur))
        {
            return "Impossible";
        }

        double arrondi = Math.Round(valeur, 12);
        if (arrondi == 0d)
        {
            arrondi = 0d;
        }

        string format = Math.Abs(arrondi) >= 1e15 || (Math.Abs(arrondi) < 1e-9 && arrondi != 0d)
            ? "0.#########E+0"
            : "0.##########";

        string texte = arrondi.ToString(format, CultureInfo.InvariantCulture).Replace('.', ',');
        if (texte.Length > 28)
        {
            texte = arrondi.ToString("0.#####E+0", CultureInfo.InvariantCulture).Replace('.', ',');
        }

        return texte;
    }
}