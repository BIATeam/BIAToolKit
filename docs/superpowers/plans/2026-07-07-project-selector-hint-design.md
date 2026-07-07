# Project selector hint & banner separation — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Remove the confusing full-overlay empty state from `GenerateUC`/`ModifyProjectUC`, replace it with a single coachmark hint anchored on the actual Root Path + Project Folder controls inside `ProjectSelectorUC`, and give the `ProjectSelectorUC` banner a visible separation from the page content below it.

**Architecture:** Pure WPF/XAML change, no view-model or business-logic change. Three files touched: `GenerateUC.xaml`, `ModifyProjectUC.xaml` (delete overlay markup only), `ProjectSelectorUC.xaml` (add a second grid row hosting the callout), `MainWindow.xaml` (wrap the shared selector in a bannered `Border`), `Themes/Surfaces.xaml` (new shared `Surface.Banner` style). All visibility logic reuses the existing `IsProjectSelected` boolean already exposed by `ProjectViewModel` (`BIA.ToolKit/ViewModels/ProjectViewModel.cs:175`) — no new property, no new converter.

**Tech Stack:** WPF, MaterialDesignInXaml, CommunityToolkit.Mvvm (unchanged).

## Global Constraints

- No new ViewModel property, no new converter — bind directly to the existing `IsProjectSelected` (`ProjectViewModel.cs:175`, also exposed identically on `ModifyProjectViewModel.cs:196`).
- No first-run/dismissible persistence — hint state is derived purely from `IsProjectSelected`.
- Do not touch `TabControl` opacity/enable triggers driven by `IsProjectSelected` in `GenerateUC.xaml` / `ModifyProjectUC.xaml` — only the overlay `Border` block is removed.
- Callout must be `IsHitTestVisible="False"` so it never intercepts clicks.
- Callout spans only columns 0–1 of `ProjectSelectorUC` (Root Path + Project Folder) — never column 2 (read-only info).
- Reuse existing visual language: `MaterialDesign.Brush.Card.Background`, `MaterialDesign.Brush.Card.Border` / `MaterialDesign.Brush.Primary`, and the `DropShadowEffect` parameters already used in `VersionAndOptionUserControl.xaml:39` (`BlurRadius=8`, `ShadowDepth=1`, `Opacity=0.18`, `Color=Black`) for the callout card; the spec's banner shadow uses `ShadowDepth=2`, `Direction=270` (see Task 3).
- No automated UI test harness exists in this project (WPF desktop app). Verification per task is `dotnet-build` (must produce zero XAML binding/compile errors) plus the manual QA checklist in Task 5's final step.

---

### Task 1: Remove empty-state overlay from `GenerateUC.xaml`

**Files:**
- Modify: `BIA.ToolKit/UserControls/GenerateUC.xaml`

**Interfaces:**
- Consumes: `IsProjectSelected` (existing binding, untouched, still drives `TabControl` opacity via `Style.Triggers`).
- Produces: nothing new — this task only deletes markup.

- [ ] **Step 1: Delete the overlay `Border` block**

In `BIA.ToolKit/UserControls/GenerateUC.xaml`, remove this entire block (it sits right after the closing `</TabControl>` and right before the two closing `</Grid>` tags at the end of the file):

```xml
            <!-- Empty-state overlay: shown only when no project is selected -->
            <Border HorizontalAlignment="Center" VerticalAlignment="Center"
                    Background="{DynamicResource MaterialDesign.Brush.Card.Background}"
                    BorderBrush="{DynamicResource MaterialDesign.Brush.Primary}"
                    BorderThickness="1"
                    CornerRadius="8"
                    Padding="32,24"
                    IsHitTestVisible="False">
                <Border.Style>
                    <Style TargetType="Border">
                        <Setter Property="Visibility" Value="Collapsed"/>
                        <Style.Triggers>
                            <DataTrigger Binding="{Binding IsProjectSelected}" Value="False">
                                <Setter Property="Visibility" Value="Visible"/>
                            </DataTrigger>
                        </Style.Triggers>
                    </Style>
                </Border.Style>
                <StackPanel Orientation="Vertical" HorizontalAlignment="Center">
                    <materialDesign:PackIcon Kind="FolderOpenOutline" Width="48" Height="48"
                                             HorizontalAlignment="Center"
                                             Foreground="{DynamicResource MaterialDesign.Brush.Primary}"/>
                    <TextBlock Text="Select a project to begin"
                               FontSize="18" FontWeight="SemiBold"
                               Margin="0,12,0,4"
                               HorizontalAlignment="Center"/>
                    <TextBlock Text="Use the project selector above to choose a project to generate from."
                               Style="{StaticResource Text.Caption}"
                               HorizontalAlignment="Center"/>
                </StackPanel>
            </Border>
```

After deletion, the file must end with:

```xml
            </TabControl>
        </Grid>
    </Grid>
</UserControl>
```

- [ ] **Step 2: Build to verify no XAML errors**

Run: `dotnet-build` (target project `BIA.ToolKit`)
Expected: build succeeds, no binding/compile errors referencing `GenerateUC.xaml`.

- [ ] **Step 3: Commit**

```bash
git add BIA.ToolKit/UserControls/GenerateUC.xaml
git commit -m "chore(ui): remove empty-state overlay from GenerateUC"
```

---

### Task 2: Remove empty-state overlay from `ModifyProjectUC.xaml`

**Files:**
- Modify: `BIA.ToolKit/UserControls/ModifyProjectUC.xaml`

**Interfaces:**
- Consumes: `IsProjectSelected` (existing binding, untouched).
- Produces: nothing new — this task only deletes markup.

- [ ] **Step 1: Delete the overlay `Border` block**

In `BIA.ToolKit/UserControls/ModifyProjectUC.xaml`, remove this entire block (it sits right after the closing `</TabControl>` and right before the two closing `</Grid>` tags and the trailing blank line + `</UserControl>`):

```xml
            <!-- Empty-state overlay: shown only when no project is selected -->
            <Border HorizontalAlignment="Center" VerticalAlignment="Center"
                    Background="{DynamicResource MaterialDesign.Brush.Card.Background}"
                    BorderBrush="{DynamicResource MaterialDesign.Brush.Primary}"
                    BorderThickness="1"
                    CornerRadius="8"
                    Padding="32,24"
                    IsHitTestVisible="False">
                <Border.Style>
                    <Style TargetType="Border">
                        <Setter Property="Visibility" Value="Collapsed"/>
                        <Style.Triggers>
                            <DataTrigger Binding="{Binding IsProjectSelected}" Value="False">
                                <Setter Property="Visibility" Value="Visible"/>
                            </DataTrigger>
                        </Style.Triggers>
                    </Style>
                </Border.Style>
                <StackPanel Orientation="Vertical" HorizontalAlignment="Center">
                    <materialDesign:PackIcon Kind="FolderOpenOutline" Width="48" Height="48"
                                             HorizontalAlignment="Center"
                                             Foreground="{DynamicResource MaterialDesign.Brush.Primary}"/>
                    <TextBlock Text="Select a project to begin"
                               FontSize="18" FontWeight="SemiBold"
                               Margin="0,12,0,4"
                               HorizontalAlignment="Center"/>
                    <TextBlock Text="Use the project selector above to choose a project to migrate."
                               Style="{StaticResource Text.Caption}"
                               HorizontalAlignment="Center"/>
                </StackPanel>
            </Border>
```

After deletion, the file must end with:

```xml
            </TabControl>
        </Grid>
    </Grid>

</UserControl>
```

- [ ] **Step 2: Build to verify no XAML errors**

Run: `dotnet-build` (target project `BIA.ToolKit`)
Expected: build succeeds, no binding/compile errors referencing `ModifyProjectUC.xaml`.

- [ ] **Step 3: Commit**

```bash
git add BIA.ToolKit/UserControls/ModifyProjectUC.xaml
git commit -m "chore(ui): remove empty-state overlay from ModifyProjectUC"
```

---

### Task 3: Add `Surface.Banner` style to `Themes/Surfaces.xaml`

**Files:**
- Modify: `BIA.ToolKit/Themes/Surfaces.xaml`

**Interfaces:**
- Consumes: `MaterialDesign.Brush.Card.Background`, `MaterialDesign.Brush.Card.Border` (existing dynamic resources, already used elsewhere in this file).
- Produces: `Surface.Banner` — a `Style` keyed resource (`TargetType="Border"`) consumed by Task 4 (`MainWindow.xaml`).

- [ ] **Step 1: Append the new style**

In `BIA.ToolKit/Themes/Surfaces.xaml`, insert the new style right after the closing `</Style>` of `Surface.TintedPanel` and before the `Tooltip.Hint` style comment:

```xml
    <!-- Banner: persistent top strip (e.g. the shared project selector) that sits
         above scrolling page content — a bottom border + downward drop shadow
         reads as "floating above" rather than a boxed card. -->
    <Style x:Key="Surface.Banner" TargetType="Border">
        <Setter Property="Background" Value="{DynamicResource MaterialDesign.Brush.Card.Background}"/>
        <Setter Property="BorderBrush" Value="{DynamicResource MaterialDesign.Brush.Card.Border}"/>
        <Setter Property="BorderThickness" Value="0,0,0,1"/>
        <Setter Property="Padding" Value="10"/>
        <Setter Property="Effect">
            <Setter.Value>
                <DropShadowEffect BlurRadius="8" ShadowDepth="2" Direction="270" Opacity="0.18" Color="Black"/>
            </Setter.Value>
        </Setter>
    </Style>

```

- [ ] **Step 2: Build to verify the resource dictionary still parses**

Run: `dotnet-build` (target project `BIA.ToolKit`)
Expected: build succeeds, no XAML parse errors referencing `Surfaces.xaml`.

- [ ] **Step 3: Commit**

```bash
git add BIA.ToolKit/Themes/Surfaces.xaml
git commit -m "feat(ui): add Surface.Banner shared style"
```

---

### Task 4: Wrap `SharedProjectSelector` in a bannered `Border` in `MainWindow.xaml`

**Files:**
- Modify: `BIA.ToolKit/MainWindow.xaml`

**Interfaces:**
- Consumes: `Surface.Banner` style (Task 3), existing `SelectedPage`/`EnumToVisibilityMultiConverter` binding (untouched).
- Produces: nothing new for later tasks — this is presentation-only.

- [ ] **Step 1: Replace the inline-margin selector with a bannered wrapper**

In `BIA.ToolKit/MainWindow.xaml`, find:

```xml
                <!-- Shared Project Selector (visible for Migration/Generation only) -->
                <uc:ProjectSelectorUC x:Name="SharedProjectSelector" Grid.Row="0" Margin="10"
                                      Visibility="{Binding DataContext.SelectedPage, RelativeSource={RelativeSource AncestorType=Window}, Converter={StaticResource EnumToVisibilityMultiConverter}, ConverterParameter='Migration,Generation', FallbackValue=Collapsed}"/>
```

Replace with:

```xml
                <!-- Shared Project Selector (visible for Migration/Generation only) -->
                <Border Grid.Row="0" Style="{StaticResource Surface.Banner}"
                        Visibility="{Binding DataContext.SelectedPage, RelativeSource={RelativeSource AncestorType=Window}, Converter={StaticResource EnumToVisibilityMultiConverter}, ConverterParameter='Migration,Generation', FallbackValue=Collapsed}">
                    <uc:ProjectSelectorUC x:Name="SharedProjectSelector"/>
                </Border>
```

Note: the `Visibility` binding moves from `ProjectSelectorUC` to the wrapping `Border` (the control itself no longer needs it); the `Margin="10"` inline on the control is dropped in favor of the `Border`'s `Padding="10"` from `Surface.Banner`.

- [ ] **Step 2: Build to verify no XAML/binding errors**

Run: `dotnet-build` (target project `BIA.ToolKit`)
Expected: build succeeds, no errors referencing `MainWindow.xaml`.

- [ ] **Step 3: Commit**

```bash
git add BIA.ToolKit/MainWindow.xaml
git commit -m "feat(ui): separate shared project selector banner from page content"
```

---

### Task 5: Add the coachmark callout to `ProjectSelectorUC.xaml`

**Files:**
- Modify: `BIA.ToolKit/UserControls/ProjectSelectorUC.xaml`

**Interfaces:**
- Consumes: `IsProjectSelected` (bound on `DataContext`, i.e. `ProjectViewModel` — `ProjectViewModel.cs:175`), `Text.Caption` style (`Themes/Typography.xaml:25`, already used elsewhere e.g. `MainWindow.xaml:113`).
- Produces: nothing new for later tasks — this is the final visual piece of the spec.

- [ ] **Step 1: Add a second row to the root `Grid`**

In `BIA.ToolKit/UserControls/ProjectSelectorUC.xaml`, find:

```xml
    <Grid>
        <Grid.ColumnDefinitions>
            <ColumnDefinition/>
            <ColumnDefinition/>
            <ColumnDefinition/>
        </Grid.ColumnDefinitions>
```

Replace with:

```xml
    <Grid>
        <Grid.ColumnDefinitions>
            <ColumnDefinition/>
            <ColumnDefinition/>
            <ColumnDefinition/>
        </Grid.ColumnDefinitions>
        <Grid.RowDefinitions>
            <RowDefinition Height="auto"/>
            <RowDefinition Height="auto"/>
        </Grid.RowDefinitions>
```

This is additive only: none of the three existing column `Grid`s (`Grid.Column="0"`, `"1"`, `"2"`) declare `Grid.Row`, so they keep defaulting to row 0 — unaffected by the new row 1.

- [ ] **Step 2: Add the callout in row 1, spanning columns 0–1**

In the same file, immediately before the final `</Grid>` that closes the root `Grid` (i.e. right after the closing `</Grid>` of the "Column 2: Read-only project info" block), insert:

```xml

        <!-- Coachmark hint: shown only while no project is selected. Anchored on the
             two controls that actually let the user pick a project (Root Path +
             Project Folder, columns 0-1) — never on the read-only info in column 2.
             IsHitTestVisible=False so it never steals clicks from the controls above it. -->
        <Border Grid.Row="1" Grid.Column="0" Grid.ColumnSpan="2"
                Margin="0,6,0,0"
                IsHitTestVisible="False">
            <Border.Style>
                <Style TargetType="Border">
                    <Setter Property="Visibility" Value="Collapsed"/>
                    <Style.Triggers>
                        <DataTrigger Binding="{Binding IsProjectSelected}" Value="False">
                            <Setter Property="Visibility" Value="Visible"/>
                        </DataTrigger>
                    </Style.Triggers>
                </Style>
            </Border.Style>
            <StackPanel HorizontalAlignment="Center">
                <Path Data="M 0,8 L 8,0 L 16,8 Z"
                      Fill="{DynamicResource MaterialDesign.Brush.Primary}"
                      Width="16" Height="8"
                      HorizontalAlignment="Center"/>
                <Border CornerRadius="6" BorderThickness="1" Padding="10,8"
                        Background="{DynamicResource MaterialDesign.Brush.Card.Background}"
                        BorderBrush="{DynamicResource MaterialDesign.Brush.Primary}">
                    <Border.Effect>
                        <DropShadowEffect BlurRadius="8" ShadowDepth="1" Opacity="0.18" Color="Black"/>
                    </Border.Effect>
                    <StackPanel Orientation="Horizontal">
                        <materialDesign:PackIcon Kind="HelpCircleOutline" Width="16" Height="16"
                                                 VerticalAlignment="Top" Margin="0,2,8,0"
                                                 Foreground="{DynamicResource MaterialDesign.Brush.Primary}"/>
                        <StackPanel>
                            <TextBlock Text="Select a project to begin" FontWeight="SemiBold"/>
                            <TextBlock Text="Choose a parent folder, then pick a project from the list above."
                                       Style="{StaticResource Text.Caption}"
                                       TextWrapping="Wrap"/>
                        </StackPanel>
                    </StackPanel>
                </Border>
            </StackPanel>
        </Border>
    </Grid>
```

(Note the trailing `</Grid>` above is the pre-existing closing tag of the root `Grid` — do not duplicate it; the inserted block ends with it.)

- [ ] **Step 3: Build to verify no XAML/binding errors**

Run: `dotnet-build` (target project `BIA.ToolKit`)
Expected: build succeeds, no errors referencing `ProjectSelectorUC.xaml`.

- [ ] **Step 4: Manual QA pass**

Launch the app (`dotnet run` on `BIA.ToolKit`, or F5 in Visual Studio) and verify, per the spec's Testing section:
1. Navigate to Migration or Generation with no project selected: no large centered overlay appears; tabs are dimmed as before.
2. The new callout appears under the Root Path + Project Folder columns only (not under the read-only info column), with up-arrow, icon, title and caption legible in both light and dark themes (use the theme-toggle button in the title bar).
3. Select a project: callout disappears immediately, tabs re-enable.
4. The `ProjectSelectorUC` banner now shows a visible bottom border + drop shadow separating it from the page content below, in both Migration and Generation pages, in both themes.

- [ ] **Step 5: Commit**

```bash
git add BIA.ToolKit/UserControls/ProjectSelectorUC.xaml
git commit -m "feat(ui): add coachmark hint anchored on project selector controls"
```
