namespace CalculatriceMaui;

public partial class MainPage : ContentPage
{
    private readonly CalculatorEngine _engine = new();
    private bool? _isLandscape;
    private const int MaxHistory = 20;

    public MainPage()
    {
        InitializeComponent();
        Refresh();
    }

    // ---------- Adaptation portrait / paysage ----------

    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);
        if (width <= 0 || height <= 0) return;

        var landscape = width > height;
        if (landscape == _isLandscape) return;
        _isLandscape = landscape;
        ApplyLayout(landscape);
    }

    private void ApplyLayout(bool landscape)
    {
        RootGrid.RowDefinitions.Clear();
        RootGrid.ColumnDefinitions.Clear();

        if (landscape)
        {
            // Affichage + fonctions à gauche, clavier à droite
            RootGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            RootGrid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(1.4, GridUnitType.Star)));
            RootGrid.RowDefinitions.Add(new RowDefinition(GridLength.Star));
            RootGrid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));

            Grid.SetRow(DisplayBorder, 0); Grid.SetColumn(DisplayBorder, 0);
            Grid.SetRow(SciRow, 1);        Grid.SetColumn(SciRow, 0);
            Grid.SetRow(Keypad, 0);        Grid.SetColumn(Keypad, 1); Grid.SetRowSpan(Keypad, 2);
        }
        else
        {
            // Empilé : affichage, fonctions, clavier
            RootGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            RootGrid.RowDefinitions.Add(new RowDefinition(GridLength.Star));
            RootGrid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            RootGrid.RowDefinitions.Add(new RowDefinition(new GridLength(2.4, GridUnitType.Star)));

            Grid.SetRow(DisplayBorder, 0); Grid.SetColumn(DisplayBorder, 0);
            Grid.SetRow(SciRow, 1);        Grid.SetColumn(SciRow, 0);
            Grid.SetRow(Keypad, 2);        Grid.SetColumn(Keypad, 0); Grid.SetRowSpan(Keypad, 1);
        }

        var fontSize = landscape ? 20 : 28;
        foreach (var button in Keypad.Children.OfType<Button>())
            button.FontSize = fontSize;

        Refresh();
    }

    // ---------- Gestionnaires d'événements ----------

    private void OnDigitClicked(object? sender, EventArgs e)
    {
        if (sender is Button b) { _engine.InputDigit(b.Text[0]); Update(); }
    }

    private void OnDecimalClicked(object? sender, EventArgs e) { _engine.InputDecimalSeparator(); Update(); }
    private void OnClearClicked(object? sender, EventArgs e) { _engine.Reset(); Update(); }
    private void OnBackspaceClicked(object? sender, EventArgs e) { _engine.Backspace(); Update(); }
    private void OnSignClicked(object? sender, EventArgs e) { _engine.ToggleSign(); Update(); }
    private void OnPercentClicked(object? sender, EventArgs e) { _engine.Percent(); Update(); }
    private void OnSqrtClicked(object? sender, EventArgs e) { _engine.SquareRoot(); Update(); }
    private void OnSquareClicked(object? sender, EventArgs e) { _engine.Square(); Update(); }
    private void OnReciprocalClicked(object? sender, EventArgs e) { _engine.Reciprocal(); Update(); }

    private void OnOperatorClicked(object? sender, EventArgs e)
    {
        if (sender is not Button b) return;
        var op = b.ClassId switch
        {
            "add" => Operation.Add,
            "sub" => Operation.Subtract,
            "mul" => Operation.Multiply,
            "div" => Operation.Divide,
            _ => Operation.None
        };
        if (op != Operation.None) { _engine.SetOperator(op); Update(); }
    }

    private void OnEqualsClicked(object? sender, EventArgs e)
    {
        _engine.Evaluate();
        if (_engine.LastCalculation is { } line) AddToHistory(line);
        Update();
    }

    private void OnHistoryClicked(object? sender, EventArgs e)
        => HistoryScroll.IsVisible = !HistoryScroll.IsVisible;

    private async void OnCopyClicked(object? sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(_engine.CopyValue)) return;
        try { await Clipboard.Default.SetTextAsync(_engine.CopyValue); }
        catch { /* presse-papiers indisponible : on ignore */ }
    }

    // ---------- Affichage ----------

    private void Update()
    {
        try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); }
        catch { /* retour haptique non supporté */ }
        Refresh();
    }

    private void Refresh()
    {
        ResultLabel.Text = _engine.DisplayEntry;
        ResultLabel.TextColor = _engine.HasError ? Colors.Salmon : Colors.White;
        ResultLabel.FontSize = ComputeResultFontSize(ResultLabel.Text.Length, _engine.HasError);
        ExpressionLabel.Text = _engine.Expression;
        _ = ScrollExpressionToEndAsync();
    }

    /// <summary>Réduit la police quand le nombre est long, pour ne jamais déborder de l'écran.</summary>
    private double ComputeResultFontSize(int length, bool isError)
    {
        var landscape = _isLandscape == true;
        var baseSize = landscape ? 38 : 60;
        if (isError) return landscape ? 18 : 24;
        return length switch
        {
            <= 8 => baseSize,
            <= 11 => baseSize * 0.8,
            <= 14 => baseSize * 0.62,
            _ => baseSize * 0.5
        };
    }

    private async Task ScrollExpressionToEndAsync()
    {
        try { await ExpressionScroll.ScrollToAsync(ExpressionLabel, ScrollToPosition.End, false); }
        catch { /* ignoré */ }
    }

    private void AddToHistory(string line)
    {
        HistoryStack.Children.Insert(0, new Label
        {
            Text = line,
            FontSize = 14,
            TextColor = Color.FromArgb("#8E8E93"),
            HorizontalTextAlignment = TextAlignment.End,
            LineBreakMode = LineBreakMode.NoWrap
        });
        while (HistoryStack.Children.Count > MaxHistory)
            HistoryStack.Children.RemoveAt(HistoryStack.Children.Count - 1);
    }
}
