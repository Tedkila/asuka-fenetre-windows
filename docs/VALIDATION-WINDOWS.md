# Recette matérielle à faire sur les deux PC

Les tests automatiques ne remplacent pas cette vérification sur Windows 10 cuisine et Windows 11 mobile. Cocher après essai réel ; aucune case n’atteste un essai déjà fait.

- [ ] Extraire le ZIP complet ; lancer sans administrateur, sans SDK .NET installé. Vérifier WebView2, le CRT local et les licences dans le paquet.
- [ ] Créer le code puis quitter/relancer. Code requis pour les réglages, aucun code par défaut. Après cinq erreurs, attendre 30 secondes avant de réessayer.
- [ ] Autoriser la caméra de bureau. Observer le voyant quand elle surveille. Tester une caméra refusée, occupée et débranchée : écran neutre, aucun plantage, code encore utilisable.
- [ ] Enregistrer Teddy seul, puis vérifier qu’un seul `profil.dpapi` chiffré est créé dans `%LOCALAPPDATA%\FenetreAsuka`. Aucun JPEG/PNG/vidéo de webcam ne doit apparaître.
- [ ] Teddy arrive : après trois observations reconnues, la page du serveur s’ouvre. Une autre personne seule, puis une autre avec Teddy : écran neutre sans page personnelle.
- [ ] Inconnu : **C’est moi**, mauvais code refuse, bon code ouvre temporairement. L’ouverture manuelle expire après le délai configuré, y compris en mode invité.
- [ ] Après absence pendant le délai (utiliser 1 minute pour l’essai), écran noir. Un mouvement réveille seulement l’écran neutre. Visage reconnu : Asuka revient.
- [ ] Activer le mode invité pendant l’affichage d’Asuka : la page disparaît et le témoin matériel caméra s’éteint. Fermer/relancer : mode invité conservé. Code exigé pour le désactiver.
- [ ] Activer une plage de sommeil couvrant l’heure présente : caméra libérée, écran noir. Tester une plage traversant minuit et la reprise en fin de plage.
- [ ] Activer maintien en activité sur secteur : observer le fonctionnement après le délai de veille Windows ; débrancher le PC mobile pour vérifier que la demande de maintien cesse. L’extinction d’écran dépend encore de Windows.
- [ ] Couper le Wi-Fi ou éteindre le serveur : message maison/Wi-Fi et Réessayer. Rétablir puis réessayer. Tester aussi une adresse refusée et une redirection vers une autre origine.
- [ ] Sur le serveur de la maison, vérifier les POST `/api/presence`, leur format, le Bearer et l’absence de toute image. Retourner 404, 500 ou ne pas répondre : aucun plantage. Sans jeton, aucune requête de présence.
- [ ] Vérifier que la page personnelle est arrêtée (y compris son audio) lorsque l’écran neutre ou la veille apparaît. Essayer les liens externes, fenêtres nouvelles, téléchargements et demandes caméra : ils doivent être bloqués.
- [ ] Activer le démarrage avec Windows, fermer puis rouvrir la session. Désactiver et recommencer. L’exécutable doit rester à son emplacement installé.
- [ ] Effacer le visage : reconnaissance automatique impossible après relance ; code toujours utilisable. Réenregistrer ensuite si souhaité.
- [ ] Quitter, lancer desinstaller.cmd, confirmer EFFACER : profil local, installation, raccourci et valeur HKCU supprimés. Vérifier ensuite une nouvelle installation avec création d’un nouveau code.

La qualité dépend de l’éclairage, des lunettes, de l’angle et de la webcam. Ajuster le seuil par petits pas : une valeur plus élevée réduit les acceptations au prix de davantage de refus. Ne pas interpréter ces essais comme une certification biométrique ou anti-usurpation.
