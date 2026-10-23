# Licences et provenance

- Application Fenêtre Asuka : MIT, voir `LICENSE`.
- Modèle **YuNet** `face_detection_yunet_2023mar.onnx` : MIT, copyright Shiqi Yu, texte intégral `models/LICENSE-YuNet`.
- Modèle **SFace** `face_recognition_sface_2021dec.onnx` : Apache-2.0, texte intégral `models/LICENSE-SFace`.
- Source des deux modèles : [OpenCV Zoo](https://github.com/opencv/opencv_zoo), commit `48b10fa505aeb51fbd127fc60a5c660040e823b7`. Fichiers sans modification. Les empreintes SHA-256 sont les OID Git LFS de ce commit. Les téléchargements et les chargements sont vérifiés.
- OpenCV et OpenCvSharp : Apache-2.0. Textes intégraux dans ce dossier. SFace utilise OpenCV DNN, avec le prétraitement [de référence OpenCV 4.13.0](https://github.com/opencv/opencv/blob/4.13.0/modules/objdetect/src/face_recognize.cpp) : alignement par similitude sur cinq repères, 112×112, RGB, échelle 1 et moyenne 0, comparaison cosinus.
- .NET : MIT et notices de ses composants, [dotnet/runtime](https://github.com/dotnet/runtime). Le paquet autonome conserve les fichiers de licence du runtime.
- Microsoft.Web.WebView2 : conditions Microsoft dans `LICENSE-WebView2`. Le Runtime WebView2 est un prérequis Microsoft distinct, non redistribué dans ce ZIP. Ses mises à jour restent sous le contrôle de Microsoft/Windows.
- DLL Microsoft Visual C++ : fichiers redistribuables officiels du répertoire `VC/Redist` de Visual Studio Build Tools du runner GitHub, copiés sans modification pour une utilisation locale sans droits administrateur. Conditions Microsoft Visual Studio et Visual C++ Redistributable applicables ; ils ne sont pas sous la licence MIT du projet.
- Le plug-in FFmpeg facultatif fourni par le paquet OpenCvSharp est omis du ZIP. L’application n’enregistre et ne décode aucune vidéo ; la caméra utilise DirectShow.

Les outils de construction téléchargent NuGet, .NET et les modèles officiels. L’application installée ne télécharge pas ces modèles. Les dépendances NuGet et leurs empreintes sont figées dans les fichiers `packages.lock.json`.
