using System.Globalization;

namespace CalculatriceMaui;

public enum Operation { None, Add, Subtract, Multiply, Divide }

/// <summary>
/// Moteur de calcul indépendant de l'interface (aucune dépendance à MAUI) :
/// il peut donc être testé seul et garde le code-behind de la page très léger.
/// </summary>
public sealed class CalculatorEngine
{
    private const int MaxDigits = 15;
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    private string _entry = "0";              // nombre en cours de saisie (séparateur décimal '.')
    private decimal? _accumulator;            // opérande gauche
    private Operation _pending = Operation.None;
    private bool _startNewEntry = true;       // le prochain chiffre remplace l'affichage (après un opérateur)
    private bool _replaceOnDigit;             // idem après %, √, x², 1/x
    private bool _justEvaluated;              // un résultat vient d'être affiché

    /// <summary>Opération en cours, affichée au-dessus du résultat.</summary>
    public string Expression { get; private set; } = "";
    public bool HasError { get; private set; }
    public string ErrorMessage { get; private set; } = "";
    /// <summary>Dernier calcul terminé (pour l'historique), null sinon.</summary>
    public string? LastCalculation { get; private set; }

    public string DisplayEntry => HasError ? ErrorMessage : _entry.Replace('.', ',');
    /// <summary>Valeur affichée, utilisable pour la copie.</summary>
    public string CopyValue => HasError ? "" : _entry;

    private decimal Value => decimal.Parse(_entry, NumberStyles.Float, Inv);

    // ---------- Saisie ----------

    public void InputDigit(char digit)
    {
        PrepareForNewNumber();
        if (_startNewEntry || _replaceOnDigit)
        {
            _entry = digit.ToString();
            _startNewEntry = false;
            _replaceOnDigit = false;
            return;
        }
        if (_entry == "0") { _entry = digit.ToString(); return; }
        if (_entry == "-0") { _entry = "-" + digit; return; }
        if (_entry.Count(char.IsDigit) < MaxDigits) _entry += digit;
    }

    public void InputDecimalSeparator()
    {
        PrepareForNewNumber();
        if (_startNewEntry || _replaceOnDigit)
        {
            _entry = "0.";
            _startNewEntry = false;
            _replaceOnDigit = false;
            return;
        }
        if (!_entry.Contains('.')) _entry += ".";
    }

    /// <summary>Remise à zéro totale (bouton C).</summary>
    public void Reset()
    {
        _entry = "0";
        _accumulator = null;
        _pending = Operation.None;
        _startNewEntry = true;
        _replaceOnDigit = false;
        _justEvaluated = false;
        HasError = false;
        ErrorMessage = "";
        Expression = "";
        LastCalculation = null;
    }

    /// <summary>Efface le dernier caractère saisi (bouton ⌫).</summary>
    public void Backspace()
    {
        if (HasError) { Reset(); return; }
        if (_justEvaluated || _startNewEntry || _replaceOnDigit) return;
        _entry = _entry.Length > 1 ? _entry[..^1] : "0";
        if (_entry is "" or "-") _entry = "0";
    }

    public void ToggleSign()
    {
        if (HasError || _entry == "0") return;
        _entry = _entry.StartsWith('-') ? _entry[1..] : "-" + _entry;
        _startNewEntry = false;
    }

    public void Percent()
    {
        if (HasError) return;
        var v = Value;
        // 200 + 10 % => 20 ; 200 × 10 % => 0,1
        var result = _pending is Operation.Add or Operation.Subtract && _accumulator is decimal acc
            ? acc * v / 100m
            : v / 100m;
        SetResult(result);
    }

    // ---------- Fonctions avancées ----------

    public void Square() => Unary(v => v * v, v => $"sqr({Show(v)})");

    public void SquareRoot()
    {
        if (HasError) return;
        if (Value < 0) { SetError("Entrée invalide", $"√({Show(Value)})"); return; }
        Unary(v => (decimal)Math.Sqrt((double)v), v => $"√({Show(v)})");
    }

    public void Reciprocal()
    {
        if (HasError) return;
        if (Value == 0) { SetError("Division par zéro impossible", $"1/({Show(0)})"); return; }
        Unary(v => 1m / v, v => $"1/({Show(v)})");
    }

    private void Unary(Func<decimal, decimal> f, Func<decimal, string> describe)
    {
        if (HasError) return;
        var v = Value;
        try
        {
            var r = f(v);
            if (_pending == Operation.None)
            {
                Expression = describe(v);
                LastCalculation = $"{Expression} = {Show(r)}";
                SetResult(r);
                _justEvaluated = true;
            }
            else SetResult(r);
        }
        catch (OverflowException) { SetError("Nombre trop grand", describe(v)); }
    }

    // ---------- Opérateurs ----------

    public void SetOperator(Operation op)
    {
        if (HasError) return;

        if (_pending != Operation.None && !_startNewEntry)
        {
            // Enchaînement : 2 + 3 × => calcule d'abord 2 + 3
            if (!TryCompute(_accumulator!.Value, _pending, Value, out var r)) return;
            _accumulator = r;
            _entry = Format(r);
        }
        else if (_pending == Operation.None)
        {
            _accumulator = Value;
        }
        // sinon : simple changement d'opérateur (2 + puis ×)

        _pending = op;
        _startNewEntry = true;
        _replaceOnDigit = false;
        _justEvaluated = false;
        LastCalculation = null;
        Expression = $"{Show(_accumulator!.Value)} {Symbol(op)}";
    }

    public void Evaluate()
    {
        if (HasError) return;
        LastCalculation = null;

        if (_pending == Operation.None)
        {
            Expression = $"{Show(Value)} =";
            _justEvaluated = true;
            return;
        }

        var a = _accumulator!.Value;
        var b = Value;
        var expr = $"{Show(a)} {Symbol(_pending)} {Show(b)} =";
        if (!TryCompute(a, _pending, b, out var r, expr)) return;

        Expression = expr;
        _entry = Format(r);
        LastCalculation = $"{expr} {Show(r)}";
        _accumulator = null;
        _pending = Operation.None;
        _justEvaluated = true;
        _startNewEntry = false;
        _replaceOnDigit = false;
    }

    // ---------- Internes ----------

    private bool TryCompute(decimal a, Operation op, decimal b, out decimal result, string? expr = null)
    {
        result = 0;
        try
        {
            result = op switch
            {
                Operation.Add => a + b,
                Operation.Subtract => a - b,
                Operation.Multiply => a * b,
                Operation.Divide => a / b,   // lève DivideByZeroException si b == 0
                _ => b
            };
            return true;
        }
        catch (DivideByZeroException)
        {
            SetError("Division par zéro impossible", expr ?? $"{Show(a)} {Symbol(op)} {Show(b)} =");
            return false;
        }
        catch (OverflowException)
        {
            SetError("Nombre trop grand", expr ?? $"{Show(a)} {Symbol(op)} {Show(b)} =");
            return false;
        }
    }

    private void SetResult(decimal value)
    {
        _entry = Format(value);
        _startNewEntry = false;
        _replaceOnDigit = true;
    }

    private void SetError(string message, string expression)
    {
        HasError = true;
        ErrorMessage = message;
        Expression = expression;
        _accumulator = null;
        _pending = Operation.None;
        _entry = "0";
        _startNewEntry = true;
        _replaceOnDigit = false;
        _justEvaluated = false;
        LastCalculation = null;
    }

    private void PrepareForNewNumber()
    {
        if (HasError || _justEvaluated) Reset();
    }

    private static string Symbol(Operation op) => op switch
    {
        Operation.Add => "+",
        Operation.Subtract => "−",
        Operation.Multiply => "×",
        Operation.Divide => "÷",
        _ => ""
    };

    private static string Show(decimal v) => Format(v).Replace('.', ',');

    private static string Format(decimal v)
    {
        v = Math.Round(v, 10, MidpointRounding.AwayFromZero);
        v /= 1.0000000000000000000000000000m;   // supprime les zéros inutiles (0,50 -> 0,5)
        if (Math.Abs(v) >= 1e15m) return ((double)v).ToString("0.#####E+0", Inv);
        return v.ToString(Inv);
    }
}
