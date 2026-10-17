using System.Windows;

namespace AionMeter.App.Services;

internal static class SettingsNavigation
{
    public static readonly DependencyProperty GroupTitleProperty = DependencyProperty.RegisterAttached(
        "GroupTitle", typeof(string), typeof(SettingsNavigation), new PropertyMetadata(null));
    public static string? GetGroupTitle(DependencyObject target) => (string?)target.GetValue(GroupTitleProperty);
    public static void SetGroupTitle(DependencyObject target, string value) => target.SetValue(GroupTitleProperty, value);

    internal static string Title(int group) => UiText.Current.Code switch
    {
        "de" => new[]{"Allgemein & Darstellung","Spielmodule","Bedienung & Daten","Updates & Hilfe"}[group],
        "fr" => new[]{"Général & apparence","Modules de jeu","Commandes & données","Mises à jour & aide"}[group],
        "ru" => new[]{"Общее и оформление","Игровые модули","Управление и данные","Обновления и помощь"}[group],
        _ => new[]{"General & appearance","Game modules","Controls & data","Updates & help"}[group]
    };
}
