# Fenêtre Asuka

Une fenêtre locale sur la maison d’Asuka et Yuna, pour Windows 10 et 11 **64 bits x64**. Serveur initial : `http://192.168.1.26:8765/`.

C# .NET 10, WinForms, WebView2 et OpenCV : interface native, sans Electron, sans compte en ligne, sans abonnement. Le ZIP embarque .NET, OpenCV et les deux modèles (environ 39 Mo pour les modèles seuls). WebView2 réutilise le moteur Microsoft déjà présent sur la plupart des PC. Ce n’est donc pas un exécutable de quelques kilo-octets. Windows ARM et 32 bits ne sont pas ciblés.

## Installer sur Windows 10 ou 11

1. Dans le dépôt GitHub, ouvrez **Actions → Fabriquer Fenêtre Asuka**. Choisissez une exécution verte, puis téléchargez l’artefact **FenetreAsuka-Windows-10-11-x64**. Il contient le ZIP de l’application et sa somme SHA-256. Le workflow démarre lors d’un envoi sur `main`, d’un tag `v…`, ou avec **Run workflow**.
2. Décompressez d’abord l’artefact puis **FenetreAsuka-win-x64.zip** dans un dossier de votre compte. Ne lancez pas l’application à l’intérieur du ZIP.
3. Double-cliquez sur **FenetreAsuka.exe**. Aucun SDK .NET ni droit administrateur n’est nécessaire. Pour une installation rangée avec raccourci dans le menu Démarrer, lancez plutôt **installer.cmd** : les fichiers seront copiés dans `%LOCALAPPDATA%\Programs\FenetreAsuka`.
4. Si WebView2 manque, installez le **Microsoft Edge WebView2 Runtime Evergreen x64** depuis le [site officiel Microsoft](https://developer.microsoft.com/microsoft-edge/webview2/#download). Le programme d’installation peut s’exécuter pour votre seul compte sans élévation ; une politique d’entreprise peut l’interdire. Gardez Windows et ce moteur à jour. Aucun téléchargement de modèle ne se fait dans l’application.
5. Créez un code personnel d’au moins **6 chiffres**. Il n’existe aucun code par défaut. La barre supérieure contient **C’est moi**, **Réglages**, **Mode invité**, **Verrouiller** et **Quitter**. Alt+F4 permet aussi de quitter : ce n’est pas une borne Windows verrouillée.
6. Sur Windows 10 : **Paramètres → Confidentialité → Caméra** ; sur Windows 11 : **Paramètres → Confidentialité et sécurité → Caméra**. Autorisez la caméra pour les applications de bureau. Fermez une autre application qui monopolise la webcam.

Une application personnelle non signée peut déclencher SmartScreen. Ne la lancez que si vous faites confiance au dépôt et à la fabrication téléchargée. Sur un poste géré, respectez la politique de votre organisation.

## Enregistrer le visage de Teddy

- Ouvrez **Réglages**, entrez le code, puis **Enregistrer / remplacer mon visage**.
- Teddy doit être seul, face à la webcam, avec un éclairage régulier. Confirmez, puis restez ainsi quelques secondes : huit observations cohérentes sont requises. L’opération expire après 30 secondes si elle échoue.
- Seule une signature de 128 nombres est conservée, chiffrée par **DPAPI pour le compte Windows courant**. Aucune photo ni vidéo n’est conservée ou envoyée. Faites un enregistrement sur chaque PC : le profil n’est pas portable entre comptes Windows.
- **Effacer mon visage**, dans ces mêmes réglages, supprime la signature persistée. Les autres réglages restent disponibles.

Trois observations successives reconnues ouvrent Asuka. Un visage inconnu ou plusieurs visages ramènent immédiatement à **Bonjour !** sans contenu personnel. **C’est moi + code** ouvre aussi Asuka, pour une durée limitée au délai de veille (3 minutes par défaut), même si la webcam est coupée ou ne reconnaît pas Teddy. Cela permet aussi de tester la page sans caméra.

La reconnaissance YuNet/SFace n’effectue pas de preuve de vie : une photo peut la tromper. Elle sert au confort de la fenêtre familiale ; utilisez une session Windows personnelle et son verrouillage pour une protection forte.

## Mode invité, veille et sommeil

- **Mode invité** masque Asuka et libère la webcam. Le réglage est conservé après redémarrage. Le code est nécessaire pour quitter ce mode ; chacun peut l’activer pour couper la surveillance.
- Le voyant **● Webcam active — traitement local** indique la surveillance, y compris sur l’écran noir. Si la caméra ou les modèles sont indisponibles, aucun déverrouillage automatique n’est possible. Ouvrez les réglages pour **Réessayer webcam**.
- Après **3 minutes sans visage** (réglable de 1 à 60), l’affichage devient noir. Un mouvement le réveille sur l’écran neutre, puis la reconnaissance décide de l’accès. Aucun mouvement seul ne dévoile Asuka.
- Il s’agit d’un écran noir avec une petite barre de contrôle et le voyant, pas d’une extinction matérielle. La webcam traite au maximum environ 3 images par seconde à 640×480 ; la détection simple de mouvement utilise une image réduite en mémoire.
- Les heures de sommeil, désactivées par défaut, utilisent l’heure locale Windows. Quand elles sont activées, l’écran devient noir et la webcam se coupe, puis reprend après la plage. Le code ne contourne pas cette plage ; vous pouvez modifier les horaires dans les réglages.
- L’option **Empêcher la veille Windows, uniquement branché** maintient le système actif, sans forcer le rétroéclairage. Elle est sans effet sur batterie. Pour garder l’écran noir matériellement allumé, ajustez séparément le délai d’extinction d’écran dans Windows. La fermeture du capot, une veille manuelle et les politiques Windows restent prioritaires.
- Le démarrage automatique est facultatif et ne concerne que la session Windows courante. Activez-le après avoir installé l’application à un emplacement stable.

## Serveur et présence

Dans **Réglages → Adresse, présence, veille et vie privée**, modifiez l’adresse, le nom de l’appareil, la pièce, le jeton, les seuils, la webcam et les horaires. L’adresse doit être une IPv4 privée locale, ou localhost, en HTTP ou HTTPS. Les noms DNS, les sites publics et les certificats HTTPS non approuvés ne sont pas acceptés. La page et ses ressources HTTP doivent rester sur la même origine (protocole, adresse et port).

Sur le Wi-Fi de la maison, la page s’affiche normalement. Hors de la maison, ou si le serveur ne répond pas, l’écran indique **« Asuka n'est joignable qu'à la maison »**, **« Vérifie le Wi-Fi de la maison »**, avec **Réessayer**. Une erreur de la page n’envoie pas vers un service externe.

Si un jeton est renseigné, l’application envoie, au plus toutes les 30 secondes, un POST à `/api/presence` de ce même serveur, sans suivre les redirections :

```http
Authorization: Bearer VOTRE_JETON
Content-Type: application/json
```

```json
{"appareil":"PC-CUISINE","piece":"Cuisine","teddy":true,"heure":"2026-10-23T18:00:00+00:00"}
```

`teddy` vaut un booléen : accès personnel ouvert après reconnaissance ou code. L’heure est UTC au format ISO 8601. Le jeton vide désactive l’envoi. Absence de réponse, délai de 3 secondes, 404, 401 ou toute autre erreur HTTP sont ignorés. Aucune file d’attente ni retransmission des événements anciens. Le serveur doit accepter ce contrat ; il n’est pas modifié par ce projet. En HTTP, le jeton circule en clair sur le réseau local ; HTTPS avec un certificat approuvé peut être utilisé si votre serveur le permet.

Le programme ne comporte ni suivi, ni publicité, ni service cloud, ni télémétrie applicative. Le traitement caméra est local et ne transmet que l’état de présence. Le contenu servi par votre serveur et les mises à jour Windows/WebView2 relèvent de leurs propres réglages ; ce projet ne garantit pas les comportements réseau du système ou du code de votre page.

## Désinstaller et tout effacer

1. Cliquez sur **Quitter**.
2. Lancez **desinstaller.cmd** depuis le dossier de l’application. Tapez **EFFACER** pour confirmer. Le script supprime l’installation par utilisateur, son raccourci, le démarrage automatique et **tout le profil local** (code, signature, jeton, réglages, profil WebView2).
3. Supprimez aussi le dossier extrait et les ZIP conservés. WebView2 partagé n’est pas désinstallé : d’autres logiciels l’utilisent.

À la main, après avoir quitté : désactivez le démarrage automatique dans l’application, puis supprimez `%LOCALAPPDATA%\FenetreAsuka`, `%LOCALAPPDATA%\Programs\FenetreAsuka` et le raccourci du menu Démarrer. Si vous avez oublié le code ou changé de compte Windows, supprimer le premier dossier réinitialise le profil et efface le visage. Une suppression classique n’est pas un effacement physique garanti sur SSD ou dans des sauvegardes.

## Développer et vérifier

Voir [DEVELOPPEMENT.md](docs/DEVELOPPEMENT.md) et [VALIDATION-WINDOWS.md](docs/VALIDATION-WINDOWS.md). Les licences des modèles sont distribuées dans `models/` : **YuNet MIT**, **SFace Apache-2.0**. Les versions et empreintes sont figées dans `models/manifest.json`. Voir aussi [LICENCES.md](docs/LICENCES.md).
