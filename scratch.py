import sys, re

filepath = r'e:\Projects\DungeonApp\Views\Sessions\SessionNotesView.axaml'
with open(filepath, 'r', encoding='utf-8') as f:
    content = f.read()

pattern = r'<Button (Tag="Format:[^"]+") Click="OnFormatButtonClick" Focusable="False" ToolTip\.Tip="([^"]+)"[\s\n]+Background="Transparent" Padding="8,6" CornerRadius="4" BorderThickness="0">[\s\n]+<Button\.Styles>[\s\n]+<Style Selector="Button:pointerover /template/ ContentPresenter">[\s\n]+<Setter Property="Background" Value="\{DynamicResource HoverGrip\}"/>[\s\n]+</Style>[\s\n]+</Button\.Styles>[\s\n]+<PathIcon Data="([^"]+)" Width="16" Height="16" Foreground="\{DynamicResource TextSecondary\}"/>[\s\n]+</Button>'

replacement = r'<Button \1 Classes="icon" Click="OnFormatButtonClick" Focusable="False" ToolTip.Tip="\2" Padding="8,6">\n                            <PathIcon Data="\3" Width="16" Height="16" Foreground="{DynamicResource TextSecondary}"/>\n                        </Button>'

content = re.sub(pattern, replacement, content)

with open(filepath, 'w', encoding='utf-8') as f:
    f.write(content)
print('Done!')
