namespace AionMeter.App.Services;

public sealed partial class UiText
{
    private static readonly Dictionary<string,(string De,string Ru,string Fr)> LayoutComparisonText=new()
    {
        ["Overlay profiles"]=("Overlay-Profile","Профили оверлеев","Profils des overlays"),
        ["Profile name"]=("Profilname","Название профиля","Nom du profil"),
        ["Compare with"]=("Vergleichen mit","Сравнить с","Comparer avec"),
        ["Current run"]=("Aktueller Lauf","Текущий бой","Combat actuel"),
        ["Last matching run"]=("Letzter passender Lauf","Последний подходящий бой","Dernier combat comparable"),
        ["Saved profiles"]=("Gespeicherte Profile","Сохранённые профили","Profils enregistrés"),
        ["Save current layout"]=("Aktuelles Layout speichern","Сохранить текущий макет","Enregistrer la disposition actuelle"),
        ["No profiles saved yet."]=("Noch keine Profile gespeichert.","Пока нет сохранённых профилей.","Aucun profil enregistré."),
        ["Save your current layout and switch between your own profiles whenever you want."]=("Speichere deine Anordnung und wechsle bei Bedarf zwischen deinen eigenen Profilen.","Сохраните расположение модулей и переключайтесь между своими профилями.","Enregistrez votre disposition et changez de profil quand vous le souhaitez."),
        ["Healing"]=("Heilung","Лечение","Soins"),
        ["Deaths"]=("Tode","Смерти","Morts"),
        ["Optional layouts. Arrange your modules first, then save a named profile. Applying closes Settings and discards unsaved edits. Other saved preferences stay unchanged."]=("Optionale Layouts. Module zuerst anordnen, dann als Profil speichern. Anwenden wirkt sofort und schließt die Einstellungen; ungespeicherte Änderungen werden verworfen. Andere Einstellungen bleiben erhalten.","Необязательные макеты. Расположите модули и сохраните профиль. Применение действует сразу и закрывает настройки; несохранённые изменения отменяются. Другие настройки сохраняются.","Disposez vos modules puis enregistrez un profil facultatif. Appliquer agit immédiatement et ferme les paramètres ; les modifications non enregistrées sont abandonnées. Les autres paramètres sont conservés."),
        ["Save new profile"]=("Neues Profil speichern","Сохранить новый профиль","Enregistrer un profil"),
        ["Apply profile"]=("Profil anwenden","Применить профиль","Appliquer le profil"),
        ["Update selected"]=("Auswahl aktualisieren","Обновить выбранный","Actualiser la sélection"),
        ["Delete selected"]=("Auswahl löschen","Удалить выбранный","Supprimer la sélection"),
        ["Enter a profile name."]=("Bitte einen Profilnamen eingeben.","Введите название профиля.","Saisissez un nom de profil."),
        ["This profile name already exists."]=("Dieser Profilname existiert bereits.","Такое название уже существует.","Ce nom existe déjà."),
        ["Could not save or apply the profile."]=("Das Profil konnte nicht gespeichert oder angewendet werden.","Не удалось сохранить или применить профиль.","Impossible d’enregistrer ou d’appliquer le profil."),
        ["Profile saved."]=("Profil gespeichert.","Профиль сохранён.","Profil enregistré."),
        ["Profile deleted."]=("Profil gelöscht.","Профиль удалён.","Profil supprimé."),
        ["Boss fight comparison"]=("Bosskampfvergleich","Сравнение боёв с боссом","Comparaison des combats de boss"),
        ["Current fight"]=("Aktueller Kampf","Текущий бой","Combat actuel"),
        ["Statistic"]=("Statistik","Показатель","Statistique"),
        ["Current"]=("Aktuell","Текущий","Actuel"),
        ["Previous"]=("Vorher","Предыдущий","Précédent"),
        ["Difference"]=("Differenz","Разница","Écart"),
        ["Duration (s)"]=("Dauer (s)","Длительность (с)","Durée (s)"),
        ["Recording"]=("Aufzeichnung","Запись","Enregistrement"),
        ["Possibly partial"]=("Evtl. unvollständig","Возможно неполная","Peut-être partiel"),
        ["Session and saved fights only. Same boss, map, character and outcome. Instance variants without distinct map IDs cannot be distinguished."]=("Nur Session- und gespeicherte Kämpfe. Gleicher Boss, Karte, Charakter und Ausgang. Varianten ohne eigene Kartenkennung lassen sich nicht unterscheiden.","Только бои сессии и сохранённые бои. Тот же босс, карта, персонаж и исход. Варианты без отдельного ID карты не различаются.","Combats de session et sauvegardés uniquement. Même boss, carte, personnage et résultat. Les variantes sans identifiant de carte distinct ne peuvent pas être distinguées."),
        ["No comparable earlier fight available. Save boss fights to compare them after restarting."]=("Kein passender früherer Kampf vorhanden. Bosskämpfe speichern, um sie auch nach einem Neustart zu vergleichen.","Нет подходящего предыдущего боя. Сохраняйте бои для сравнения после перезапуска.","Aucun combat antérieur comparable. Sauvegardez les combats pour les comparer après un redémarrage.")
    };
}
