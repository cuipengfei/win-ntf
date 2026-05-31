#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$repo_root"

xaml="src/WinNtf.App/PopupWindow.xaml"
code="src/WinNtf.App/PopupWindow.xaml.cs"

fail() {
  printf 'FAIL %s\n' "$1" >&2
  exit 1
}

grep -q 'TextOptions.TextFormattingMode="Display"' "$xaml" \
  || fail 'popup must enable display text formatting'

grep -q 'TextOptions.TextRenderingMode="ClearType"' "$xaml" \
  || fail 'popup must enable ClearType text rendering'

grep -q 'x:Name="StateBorder"' "$xaml" \
  || fail 'popup must expose state-colored border'

grep -q 'x:Name="IconContainer"' "$xaml" \
  || fail 'popup icon must have a container'

grep -q '<Style TargetType="Button"' "$xaml" \
  || fail 'close button must have hover styling'

grep -q 'EventTrigger RoutedEvent="Window.Loaded"' "$xaml" \
  || fail 'popup must have a lightweight loaded animation'

grep -q '<Grid.ColumnDefinitions>' "$xaml" \
  || fail 'popup content must use a column grid layout'

grep -q 'BorderThickness="1"' "$xaml" \
  || fail 'popup card must use a light border treatment'

if grep -q 'Width="5" HorizontalAlignment="Left"' "$xaml"; then
  fail 'popup must not keep the old 5px accent bar layout'
fi

grep -q 'BlurRadius="12"' "$xaml" \
  || fail 'popup shadow blur must be reduced'

grep -q 'Opacity="0.25"' "$xaml" \
  || fail 'popup shadow opacity must be reduced'

grep -q 'FontSize="12"' "$xaml" \
  || fail 'popup body text must use 12px font size'

grep -q 'Foreground="#9CA3AF"' "$xaml" \
  || fail 'popup body text must use stable neutral gray'

grep -q 'StateBorder.BorderBrush = BrushFor(notification.Color)' "$code" \
  || fail 'popup state border must follow notification color'

grep -q 'IconContainer.Background = TintedBrushFor(notification.Color)' "$code" \
  || fail 'popup icon container must follow notification color'

grep -q 'Variant = "success"' scripts/smoke-win.ps1 \
  || fail 'smoke script must expose notification variant for UI screenshots'

printf 'PASS popup UI contract\n'
