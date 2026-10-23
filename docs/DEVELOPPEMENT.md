# Développement

## Organisation

- `src/Asuka.Core` : réglages, restrictions d’adresse, hachage du code, politique d’accès et présence HTTP, sans dépendance Windows.
- `src/Asuka.Windows` : WinForms/WebView2, capture DirectShow, YuNet, SFace via OpenCV DNN, DPAPI, démarrage par HKCU et maintien en activité sur secteur.
- `tests/Asuka.Tests` : tests exécutables sur Linux et Windows.
- `tests/Asuka.Models.Tests` : tests réels YuNet/SFace sur CPU (Linux et Windows), contrôle des empreintes et de l’alignement.
- `tests/Asuka.Windows.Tests` : tests Windows de DPAPI et du moteur WebView2 avec une page locale de test ; aucune webcam ni donnée biométrique réelle.
- `scripts/fetch-models.py` : téléchargement **à la construction uniquement**, HTTPS et empreintes SHA-256 immuables. Le chargement dans l’application vérifie à nouveau chaque empreinte.

## Environnement cloud Linux x64

Chaque tâche cloud est déjà isolée : utiliser le checkout existant `/workspace/asuka-fenetre-windows`, sans créer de worktree. Ne pas modifier l’autre dépôt présent dans l’environnement.

```bash
./scripts/setup-cloud.sh
```

Le script installe le SDK .NET 10.0.100 localement s’il manque, restaure les versions verrouillées, compile la cible Windows, lance les tests portables, prépare les modèles puis teste réellement leur inférence sur CPU. Aucun serveur n’est à démarrer dans le cloud. L’interface, DirectShow et DPAPI ne peuvent pas s’exécuter sous Linux. Le serveur privé `192.168.1.26` ne doit pas être recherché depuis le cloud.

Pour les commandes manuelles après installation :

```bash
export DOTNET_CLI_HOME="$PWD/.tools/cli"
export NUGET_PACKAGES="$PWD/.tools/nuget"
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export PATH="$PWD/.tools/dotnet:$PATH"
dotnet test tests/Asuka.Tests -c Release --no-restore
dotnet build src/Asuka.Windows -c Release --no-restore
```

Domaines de construction : `builds.dotnet.microsoft.com`, `api.nuget.org` et ses destinations CDN, `media.githubusercontent.com` pour les modèles. GitHub Actions utilise aussi les services officiels GitHub et Microsoft pour ses outils. Pas de secret applicatif nécessaire dans le cloud, ni de signature faciale, ni de jeton de la maison dans GitHub.

## Windows x64

Installer le SDK .NET 10 (le `global.json` choisit la branche 10.0.1xx), Python 3, et WebView2. Pour développer :

```powershell
python scripts/fetch-models.py
dotnet restore tests/Asuka.Windows.Tests --locked-mode
dotnet run --project src/Asuka.Windows
```

Les tests DPAPI refusent de remplacer un profil existant. Les exécuter sur un compte Windows de test neuf, comme le runner GitHub. Les tests WebView2 ouvrent une page uniquement locale et ferment leur fenêtre en fin d’essai.

```powershell
dotnet test tests/Asuka.Tests -c Release
dotnet test tests/Asuka.Models.Tests -c Release
dotnet test tests/Asuka.Windows.Tests -c Release
```

Pour produire le ZIP autonome : Visual Studio Build Tools avec les outils C++ x64 est nécessaire afin de redistribuer le CRT autorisé, puis :

```powershell
./scripts/build-windows.ps1
```

Le workflow GitHub Actions prépare ces outils sur `windows-2022` et produit `artifacts/FenetreAsuka-win-x64.zip` et son SHA-256. Il exécute les tests avant le paquetage. Les DLL CRT sont livrées à côté de l’exécutable ; aucun installateur VC++ administrateur n’est nécessaire. L’application est volontairement non compactée et non rognée pour préserver WinForms, WebView2 et les appels natifs.

Les images caméra restent des matrices natives en mémoire, détenues par un seul worker. Elles ne sont jamais sérialisées, affichées dans le navigateur ou incluses dans une requête réseau. Les échantillons d’enregistrement temporaires ne quittent pas la mémoire. Un défaut caméra ferme l’accès automatique ; le code reste une solution de secours temporaire.
