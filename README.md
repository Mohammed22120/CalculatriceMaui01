# Calculatrice mobile — .NET MAUI

Activité n°4 – Atelier de développement mobile. Calculatrice complète (opérations de base + fonctions avancées) avec interface adaptative portrait / paysage.

Fonctionnalités
- Opérations : + − × ÷, nombres décimaux, C (remise à zéro), ⌫ (dernier caractère), ± (changement de signe), % (pourcentage)
- Opération en cours affichée au-dessus du résultat
- Division par zéro et valeurs invalides : message explicite, aucun plantage
- Avancé : √, x², 1/x, historique des calculs, copie du résultat, retour haptique, descriptions d'accessibilité
- Interface adaptative : réorganisation en deux colonnes en paysage, police du résultat réduite pour les grands nombres

Layouts utilisés 
| Layout | Rôle |
|---|---|
| Grid | Page (3 zones, 2 colonnes en paysage) et clavier 5 × 4 |
| Border | Panneau d'affichage arrondi |
| VerticalStackLayout | Empile barre d'outils, historique, opération, résultat |
| HorizontalStackLayout | Boutons « Historique » et « Copier » côte à côte |
| ScrollView | Historique (vertical) et opération longue (horizontal) |
| FlexLayout | Rangée √ / x² / 1/x qui se partage la largeur |

Mise en place du projet
Le dépôt contient les fichiers spécifiques à l'application ; le reste (Platforms, Resources, MauiProgram.cs…) vient du modèle.

```bash
dotnet new maui -n CalculatriceMaui
cd CalculatriceMaui
# Remplacer par les fichiers de ce dépôt :
#   CalculatorEngine.cs, MainPage.xaml, MainPage.xaml.cs, App.xaml.cs
dotnet build -t:Run -f net10.0-android     # ou net10.0-windows10.0.19041.0 sous Windows
```

Si le modèle génère `AppShell`, il peut rester inutilisé (`App.xaml.cs` ouvre directement `MainPage`).

Tests du moteur de calcul
Le moteur (`CalculatorEngine.cs`) ne dépend pas de MAUI et se teste en console :

```bash
cd tests/EngineCheck
dotnet run      # 32 vérifications
```
 Structure
```
CalculatriceMaui/   CalculatorEngine.cs · MainPage.xaml(.cs) · App.xaml.cs
tests/EngineCheck/  tests console du moteur
```
