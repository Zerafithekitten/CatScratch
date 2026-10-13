using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Threading;
using AionMeter.Core;
using AionMeter.Core.Combat;

namespace AionMeter.App.Services;

/// <summary>
/// "To chat" for the overlay and the breakdown: one player's result or the party ranking as a single line for the game
/// chat, or the ranking as a table for Discord. A short note confirms what landed on the clipboard.
/// </summary>
public static class ChatCopy
{
    private static UiText T => UiText.Current;

    /// <summary>The menu entries; <paramref name="fight"/> is read when one is clicked, so a live fight copies its latest numbers.</summary>
    public static IEnumerable<MenuItem> Items(FrameworkElement noteAnchor, Func<EncounterSnapshot?> fight, uint? player, string? playerName)
    {
        yield return Item(T.Localize("Copy full fight damage (numbers + %)"), s => ChatLine.DamageReport(s, T.Chat, Name));
        var chat = new MenuItem { Header = T.Localize("Copy game-chat lines (numbers + %)"), IsEnabled = fight() is not null };
        chat.Items.Add(new MenuItem { Header = T.Localize("Open to choose a line"), IsEnabled = false });
        chat.SubmenuOpened += (_, _) =>
        {
            chat.Items.Clear();
            if (fight() is not { } snapshot) return;
            var lines = ChatLine.DamageLines(snapshot with { Title = T.FightTitle(snapshot) }, T.Chat, Name);
            for (var i = 0; i < lines.Count; i++)
            {
                var text = lines[i];
                var line = new MenuItem { Header = string.Format(T.Localize("Copy line {0} of {1}"),i+1,lines.Count), ToolTip = text };
                line.Click += (_, _) => Copy(noteAnchor, text);
                chat.Items.Add(line);
            }
        };
        yield return chat;
        if (player is { } id && playerName is not null)
            yield return Item(string.Format(T.CopyPlayer, playerName), s => ChatLine.Player(s, id, T.Chat, Name));
        yield return Item(T.CopyParty, s => ChatLine.Party(s, T.Chat, Name));
        yield return Item(T.CopyTable, s => ChatLine.Table(s, T.Chat, Name));

        MenuItem Item(string header, Func<EncounterSnapshot, string> text)
        {
            var item = new MenuItem { Header = header, IsEnabled = fight() is not null };
            item.Click += (_, _) =>
            {
                if (fight() is { } s) Copy(noteAnchor, text(s with { Title = T.FightTitle(s) }));
            };
            return item;
        }
    }

    /// <summary>A small menu of <see cref="Items"/> under <paramref name="button"/>.</summary>
    public static void ShowMenu(FrameworkElement button, FrameworkElement noteAnchor, Func<EncounterSnapshot?> fight, uint? player, string? playerName)
    {
        var menu = new ContextMenu { PlacementTarget = button, Placement = PlacementMode.Bottom };
        foreach (var item in Items(noteAnchor, fight, player, playerName)) menu.Items.Add(item);
        menu.IsOpen = true;
    }

    /// <summary>Unnamed players read as "Templar #10388", never as a bare id.</summary>
    private static string Name(CombatantSnapshot c) => T.CombatantLabel(c.ActorId, c.Name, c.Class);

    private static void Copy(FrameworkElement anchor, string text) => ShowNote(anchor, AppHost.CopyText(text) ? T.Copied : T.CopyFailed);

    /// <summary>A note over <paramref name="anchor"/> that goes away by itself.</summary>
    private static void ShowNote(FrameworkElement anchor, string text)
    {
        var popup = new Popup
        {
            PlacementTarget = anchor,
            Placement = PlacementMode.Center,
            AllowsTransparency = true,
            PopupAnimation = PopupAnimation.Fade,
            StaysOpen = true,
            Child = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(0xF2, 0x14, 0x18, 0x24)),
                BorderBrush = (Brush)Application.Current.FindResource("Gold"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(7),
                Padding = new Thickness(12, 6, 12, 7),
                Child = new TextBlock
                {
                    Text = text,
                    Foreground = Brushes.White,
                    FontSize = 12.5,
                    FontFamily = (FontFamily)Application.Current.FindResource("UiFont"),
                },
            },
        };
        popup.IsOpen = true;
        var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2.5) };
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            popup.IsOpen = false;
        };
        timer.Start();
    }
}
