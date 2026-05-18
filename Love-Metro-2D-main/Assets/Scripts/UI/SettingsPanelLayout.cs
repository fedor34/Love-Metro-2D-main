using TMPro;
using UnityEngine;

internal static class SettingsPanelLayout
{
    private const float ReferenceWidth = 1530f;
    private const float ReferenceHeight = 980f;

    public static void Apply(RectTransform panel)
    {
        if (panel == null)
            return;

        RectTransform parent = panel.parent as RectTransform;
        float canvasWidth = panel.rect.width > 1f ? panel.rect.width : parent != null ? parent.rect.width : ReferenceWidth;
        float canvasHeight = panel.rect.height > 1f ? panel.rect.height : parent != null ? parent.rect.height : ReferenceHeight;
        if (canvasWidth <= 1f || canvasHeight <= 1f)
            return;

        float frameMarginX = Mathf.Clamp(canvasWidth * 0.035f, 28f, 90f);
        float frameTop = Mathf.Clamp(canvasHeight * 0.025f, 18f, 44f);
        float frameBottom = Mathf.Clamp(canvasHeight * 0.035f, 24f, 58f);
        float frameWidth = canvasWidth - frameMarginX * 2f;
        float frameHeight = canvasHeight - frameTop - frameBottom;
        float scale = Mathf.Clamp(Mathf.Min(frameWidth / ReferenceWidth, frameHeight / ReferenceHeight), 0.68f, 1f);

        SetStretch(panel, 0f, 0f, 0f, 0f);
        SetStretch(Find(panel, "ReferenceFrame"), frameMarginX, frameMarginX, frameTop, frameBottom);

        RectTransform frame = Find(panel, "ReferenceFrame");
        if (frame == null)
            return;

        LayoutFrame(frame, frameWidth, frameHeight, scale);
        ConfigureText(frame, scale);
    }

    private static void LayoutFrame(RectTransform frame, float width, float height, float scale)
    {
        float pad = Mathf.Clamp(width * 0.02f, 22f, 34f);
        float headerHeight = Mathf.Clamp(height * 0.105f, 68f, 96f);
        float footerHeight = Mathf.Clamp(height * 0.075f, 54f, 72f);
        float footerBottom = Mathf.Clamp(height * 0.028f, 20f, 34f);
        float contentTop = headerHeight + Mathf.Clamp(height * 0.03f, 20f, 32f);
        float contentBottom = footerHeight + footerBottom + Mathf.Clamp(height * 0.028f, 22f, 34f);
        float contentHeight = Mathf.Max(360f, height - contentTop - contentBottom);
        float navWidth = Mathf.Clamp(width * 0.205f, 195f, 300f);
        float gap = Mathf.Clamp(width * 0.018f, 18f, 30f);

        SetStretch(Find(frame, "FrameBackground"), 8f, 8f, 8f, 8f);
        SetStretch(Find(frame, "FrameShadow"), -10f, -10f, -10f, -10f);
        SetStretchTop(Find(frame, "Header"), 8f, 8f, 8f, headerHeight);
        SetStretchTop(Find(frame, "HeaderLine"), 8f, 8f, headerHeight + 8f, 5f);
        SetTopLeft(Find(frame, "TitleShadow"), pad + 4f, 27f * scale + 4f, Mathf.Min(480f, width * 0.48f), 58f * scale);
        SetTopLeft(Find(frame, "Title"), pad, 27f * scale, Mathf.Min(480f, width * 0.48f), 58f * scale);

        RectTransform tabs = Find(frame, "CategoryTabs");
        SetTopLeft(tabs, pad, contentTop, navWidth, contentHeight);
        LayoutTabs(tabs, navWidth, contentHeight, scale);

        float contentLeft = pad + navWidth + gap;
        float contentRight = pad;
        RectTransform content = Find(frame, "SettingsContent");
        SetStretch(content, contentLeft, contentRight, contentTop, contentBottom);
        LayoutContent(content, width - contentLeft - contentRight, contentHeight, scale);

        LayoutFooter(frame, width, footerHeight, footerBottom, pad, gap, scale);
    }

    private static void LayoutTabs(RectTransform tabs, float width, float height, float scale)
    {
        if (tabs == null)
            return;

        string[] names = { "SoundTab", "GraphicsTab", "ControlsTab", "GameTab" };
        float tabHeight = Mathf.Clamp(height * 0.15f, 70f, 98f);
        float gap = Mathf.Clamp(height * 0.055f, 24f, 42f);
        float y = 0f;

        foreach (string name in names)
        {
            RectTransform tab = Find(tabs, name);
            SetTopLeft(tab, 0f, y, width, tabHeight);
            SetStretch(Find(tab, "Shadow"), 8f, -8f, 8f, -8f);
            SetStretch(Find(tab, "Body"), 8f, 8f, 8f, 8f);
            SetTopLeft(Find(tab, "LabelShadow"), 82f * scale + 4f, (tabHeight - 36f * scale) * 0.5f + 4f, width - 95f * scale, 38f * scale);
            SetTopLeft(Find(tab, "Label"), 82f * scale, (tabHeight - 36f * scale) * 0.5f, width - 95f * scale, 38f * scale);
            y += tabHeight + gap;
        }
    }

    private static void LayoutContent(RectTransform content, float width, float height, float scale)
    {
        if (content == null)
            return;

        float gap = Mathf.Clamp(height * 0.025f, 12f, 20f);
        float available = Mathf.Max(320f, height - gap * 2f);
        float soundHeight = Mathf.Clamp(available * 0.28f, 150f, 226f);
        float graphicsHeight = Mathf.Clamp(available * 0.50f, 260f, 390f);
        float otherHeight = available - soundHeight - graphicsHeight;

        if (otherHeight < 100f)
        {
            float deficit = 100f - otherHeight;
            soundHeight -= deficit * 0.35f;
            graphicsHeight -= deficit * 0.65f;
            otherHeight = 100f;
        }

        RectTransform sound = Find(content, "SoundSection");
        RectTransform graphics = Find(content, "GraphicsSection");
        RectTransform other = Find(content, "OtherSection");
        SetStretchTop(sound, 0f, 0f, 0f, soundHeight);
        SetStretchTop(graphics, 0f, 0f, soundHeight + gap, graphicsHeight);
        SetStretchTop(other, 0f, 0f, soundHeight + gap + graphicsHeight + gap, otherHeight);

        LayoutSectionFrame(sound, scale);
        LayoutSectionFrame(graphics, scale);
        LayoutSectionFrame(other, scale);
        LayoutSound(sound, width, soundHeight, scale);
        LayoutGraphics(graphics, width, graphicsHeight, scale);
        LayoutOther(other, width, otherHeight, scale);
    }

    private static void LayoutSectionFrame(RectTransform section, float scale)
    {
        if (section == null)
            return;

        SetStretch(Find(section, "Body"), 6f, 6f, 6f, 6f);
        SetStretchTop(Find(section, "Top"), 6f, 6f, 6f, 5f);
        SetStretchBottom(Find(section, "Bottom"), 6f, 6f, 6f, 5f);
        SetStretchLeft(Find(section, "Left"), 6f, 6f, 6f, 5f);
        SetStretchRight(Find(section, "Right"), 6f, 6f, 6f, 5f);
        SetTopLeft(Find(section, "Bullet"), 30f * scale, 23f * scale, 14f * scale, 14f * scale);
        SetTopLeft(Find(section, "TitleShadow"), 60f * scale + 4f, 13f * scale + 4f, 260f * scale, 36f * scale);
        SetTopLeft(Find(section, "Title"), 60f * scale, 13f * scale, 260f * scale, 36f * scale);
    }

    private static void LayoutSound(RectTransform section, float width, float height, float scale)
    {
        if (section == null)
            return;

        string[] rows = { "MasterVolume", "MusicVolume", "SfxVolume" };
        float header = Mathf.Clamp(height * 0.28f, 44f, 58f);
        float rowStep = Mathf.Max(36f, (height - header - 18f) / rows.Length);
        for (int i = 0; i < rows.Length; i++)
            LayoutSliderRow(section, rows[i], header + i * rowStep, width, scale, percentValue: true);
    }

    private static void LayoutGraphics(RectTransform section, float width, float height, float scale)
    {
        if (section == null)
            return;

        float header = Mathf.Clamp(height * 0.17f, 42f, 58f);
        float rowStep = Mathf.Max(36f, (height - header - 18f) / 5f);
        LayoutDropdownRow(section, "ResolutionDropdown", header, width, scale);
        LayoutDropdownRow(section, "QualityDropdown", header + rowStep, width, scale);
        LayoutToggleRow(section, "FullscreenToggle", header + rowStep * 2f, width, scale);
        LayoutToggleRow(section, "VsyncToggle", header + rowStep * 3f, width, scale);
        LayoutSliderRow(section, "GameSpeed", header + rowStep * 4f, width, scale, percentValue: false);
    }

    private static void LayoutOther(RectTransform section, float width, float height, float scale)
    {
        if (section == null)
            return;

        float y = Mathf.Clamp(height * 0.52f, 52f, 74f);
        float pad = 30f * scale;
        float gap = Mathf.Clamp(width * 0.04f, 24f, 44f);
        float columnWidth = (width - pad * 2f - gap) * 0.5f;
        LayoutCompactToggle(section, "MuteToggle", pad, y, columnWidth, scale);
        LayoutCompactToggle(section, "DebugModeToggle", pad + columnWidth + gap, y, columnWidth, scale);
    }

    private static void LayoutSliderRow(RectTransform section, string prefix, float y, float width, float scale, bool percentValue)
    {
        float rowHeight = Mathf.Clamp(34f * scale, 24f, 34f);
        float button = Mathf.Clamp(48f * scale, 34f, 52f);
        float gap = Mathf.Clamp(10f * scale, 7f, 12f);
        float rightPad = Mathf.Clamp(24f * scale, 16f, 26f);
        float valueWidth = Mathf.Clamp(width * 0.095f, percentValue ? 72f : 78f, percentValue ? 112f : 118f);
        float controlX = Mathf.Clamp(width * 0.43f, 300f * scale, width - 460f * scale);
        float valueX = width - rightPad - valueWidth;
        float plusX = valueX - gap - button;
        float sliderX = controlX + button + gap;
        float sliderWidth = plusX - gap - sliderX;

        if (sliderWidth < 160f * scale)
        {
            sliderWidth = 160f * scale;
            sliderX = plusX - gap - sliderWidth;
            controlX = sliderX - gap - button;
        }

        float labelWidth = Mathf.Max(140f, controlX - 42f * scale);
        SetTopLeft(Find(section, prefix + "LabelShadow"), 34f * scale + 4f, y + 4f, labelWidth, rowHeight);
        SetTopLeft(Find(section, prefix + "Label"), 34f * scale, y, labelWidth, rowHeight);
        SetTopLeft(Find(section, prefix + "Minus"), controlX, y - 5f * scale, button, button);
        SetTopLeft(Find(section, prefix + "Slider"), sliderX, y - 1f * scale, sliderWidth, rowHeight);
        SetTopLeft(Find(section, prefix + "Plus"), plusX, y - 5f * scale, button, button);
        SetTopLeft(Find(section, prefix + "ValueBox"), valueX, y - 5f * scale, valueWidth, button);
        SetTopLeft(Find(section, prefix + "ValueShadow"), valueX + 3f, y, valueWidth - 6f, rowHeight);
        SetTopLeft(Find(section, prefix + "Value"), valueX, y - 3f * scale, valueWidth, rowHeight);
        StretchButtonLabel(section, prefix + "Minus");
        StretchButtonLabel(section, prefix + "Plus");
    }

    private static void LayoutDropdownRow(RectTransform section, string prefix, float y, float width, float scale)
    {
        float rowHeight = Mathf.Clamp(40f * scale, 30f, 42f);
        float button = Mathf.Clamp(48f * scale, 34f, 52f);
        float gap = Mathf.Clamp(10f * scale, 7f, 12f);
        float rightPad = Mathf.Clamp(24f * scale, 16f, 26f);
        float controlX = Mathf.Clamp(width * 0.43f, 300f * scale, width - 560f * scale);
        float nextX = width - rightPad - button;
        float dropdownX = controlX + button + gap;
        float dropdownWidth = nextX - gap - dropdownX;

        if (dropdownWidth < 250f * scale)
        {
            dropdownWidth = 250f * scale;
            dropdownX = nextX - gap - dropdownWidth;
            controlX = dropdownX - gap - button;
        }

        float labelWidth = Mathf.Max(140f, controlX - 42f * scale);
        SetTopLeft(Find(section, prefix + "LabelShadow"), 34f * scale + 4f, y + 4f, labelWidth, rowHeight);
        SetTopLeft(Find(section, prefix + "Label"), 34f * scale, y, labelWidth, rowHeight);
        SetTopLeft(Find(section, prefix + "Previous"), controlX, y - 5f * scale, button, button);
        SetTopLeft(Find(section, prefix), dropdownX, y - 5f * scale, dropdownWidth, button);
        SetTopLeft(Find(section, prefix + "Next"), nextX, y - 5f * scale, button, button);
        StretchButtonLabel(section, prefix + "Previous");
        StretchButtonLabel(section, prefix + "Next");
    }

    private static void LayoutToggleRow(RectTransform section, string prefix, float y, float width, float scale)
    {
        float rowHeight = Mathf.Clamp(34f * scale, 24f, 34f);
        float box = Mathf.Clamp(42f * scale, 30f, 44f);
        float controlX = Mathf.Clamp(width * 0.43f, 300f * scale, width - 320f * scale);
        float labelWidth = Mathf.Max(140f, controlX - 42f * scale);

        SetTopLeft(Find(section, prefix + "LabelShadow"), 34f * scale + 4f, y + 4f, labelWidth, rowHeight);
        SetTopLeft(Find(section, prefix + "Label"), 34f * scale, y, labelWidth, rowHeight);
        SetTopLeft(Find(section, prefix), controlX, y - 5f * scale, box, box);
        SetTopLeft(Find(section, prefix + "/StatusShadow"), box + 28f * scale + 4f, 9f * scale, width - controlX - box - 52f * scale, rowHeight);
        SetTopLeft(Find(section, prefix + "/Status"), box + 28f * scale, 5f * scale, width - controlX - box - 52f * scale, rowHeight);
    }

    private static void LayoutCompactToggle(RectTransform section, string prefix, float x, float y, float width, float scale)
    {
        float rowHeight = Mathf.Clamp(34f * scale, 24f, 34f);
        float box = Mathf.Clamp(42f * scale, 30f, 44f);
        float labelWidth = Mathf.Max(110f, width * 0.52f);
        float toggleX = x + labelWidth + 16f * scale;
        float statusX = toggleX + box + 22f * scale;
        float statusWidth = Mathf.Max(70f, x + width - statusX);

        SetTopLeft(Find(section, prefix + "LabelShadow"), x + 4f, y + 4f, labelWidth, rowHeight);
        SetTopLeft(Find(section, prefix + "Label"), x, y, labelWidth, rowHeight);
        SetTopLeft(Find(section, prefix), toggleX, y - 5f * scale, box, box);
        SetTopLeft(Find(section, prefix + "/StatusShadow"), box + 22f * scale + 4f, 9f * scale, statusWidth, rowHeight);
        SetTopLeft(Find(section, prefix + "/Status"), box + 22f * scale, 5f * scale, statusWidth, rowHeight);
    }

    private static void LayoutFooter(RectTransform frame, float width, float height, float bottom, float pad, float gap, float scale)
    {
        float buttonGap = Mathf.Clamp(width * 0.045f, 24f, 58f);
        float buttonWidth = Mathf.Min(410f, (width - pad * 2f - buttonGap * 2f) / 3f);
        float defaultsWidth = Mathf.Min(500f, buttonWidth + 70f);
        buttonWidth = Mathf.Min(buttonWidth, (width - pad * 2f - buttonGap * 2f - (defaultsWidth - buttonWidth)) / 3f);

        SetBottomLeft(Find(frame, "BackButton"), pad, bottom, buttonWidth, height);
        SetBottomCenter(Find(frame, "DefaultsButton"), 0f, bottom, defaultsWidth, height);
        SetBottomRight(Find(frame, "ApplyButton"), -pad, bottom, buttonWidth, height);

        foreach (string name in new[] { "BackButton", "DefaultsButton", "ApplyButton" })
        {
            RectTransform button = Find(frame, name);
            SetStretch(Find(button, "Shadow"), 8f, -8f, 8f, -8f);
            SetStretchTop(Find(button, "Top"), 8f, 8f, 8f, 5f);
            SetStretchBottom(Find(button, "Bottom"), 8f, 8f, 8f, 5f);
            SetStretch(Find(button, "LabelShadow"), 8f, 8f, 4f, 4f);
            SetStretch(Find(button, "Label"), 8f, 8f, 8f, 8f);
        }
    }

    private static void ConfigureText(RectTransform root, float scale)
    {
        foreach (TMP_Text text in root.GetComponentsInChildren<TMP_Text>(true))
        {
            text.enableWordWrapping = false;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.enableAutoSizing = false;

            string path = BuildPath(root, text.rectTransform);
            if (path == "Title" || path == "TitleShadow")
                text.fontSize = Mathf.Round(45f * scale);
            else if (path.Contains("CategoryTabs"))
                text.fontSize = Mathf.Round((path.Contains("ControlsTab") ? 24f : 28f) * scale);
            else if (path.Contains("BackButton") || path.Contains("DefaultsButton") || path.Contains("ApplyButton"))
                text.fontSize = Mathf.Round(28f * scale);
            else if (path.Contains("Value"))
                text.fontSize = Mathf.Round(25f * scale);
            else if (path.Contains("Status"))
                text.fontSize = Mathf.Round(24f * scale);
            else if (path.EndsWith("/Title") || path.EndsWith("/TitleShadow"))
                text.fontSize = Mathf.Round(28f * scale);
            else
                text.fontSize = Mathf.Round(26f * scale);

            if (path.Contains("Button") || path.Contains("Minus") || path.Contains("Plus") || path.Contains("Previous") || path.Contains("Next") || path.Contains("Value"))
                text.alignment = TextAlignmentOptions.Center;
            else
                text.alignment = TextAlignmentOptions.MidlineLeft;
        }
    }

    private static void StretchButtonLabel(RectTransform root, string buttonName)
    {
        RectTransform button = Find(root, buttonName);
        SetStretch(Find(button, "LabelShadow"), 0f, 0f, 0f, 0f);
        SetStretch(Find(button, "Label"), 0f, 0f, 0f, 0f);
    }

    private static RectTransform Find(RectTransform root, string path)
    {
        if (root == null || string.IsNullOrEmpty(path))
            return null;

        return root.Find(path) as RectTransform;
    }

    private static void SetStretch(RectTransform rect, float left, float right, float top, float bottom)
    {
        if (rect == null)
            return;

        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = new Vector2(left, bottom);
        rect.offsetMax = new Vector2(-right, -top);
    }

    private static void SetStretchTop(RectTransform rect, float left, float right, float top, float height)
    {
        if (rect == null)
            return;

        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2((left - right) * 0.5f, -top);
        rect.sizeDelta = new Vector2(-(left + right), height);
    }

    private static void SetStretchBottom(RectTransform rect, float left, float right, float bottom, float height)
    {
        if (rect == null)
            return;

        rect.anchorMin = Vector2.zero;
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = new Vector2((left - right) * 0.5f, bottom);
        rect.sizeDelta = new Vector2(-(left + right), height);
    }

    private static void SetStretchLeft(RectTransform rect, float left, float top, float bottom, float width)
    {
        if (rect == null)
            return;

        rect.anchorMin = Vector2.zero;
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 0.5f);
        rect.anchoredPosition = new Vector2(left, (bottom - top) * 0.5f);
        rect.sizeDelta = new Vector2(width, -(top + bottom));
    }

    private static void SetStretchRight(RectTransform rect, float right, float top, float bottom, float width)
    {
        if (rect == null)
            return;

        rect.anchorMin = new Vector2(1f, 0f);
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(1f, 0.5f);
        rect.anchoredPosition = new Vector2(-right, (bottom - top) * 0.5f);
        rect.sizeDelta = new Vector2(width, -(top + bottom));
    }

    private static void SetTopLeft(RectTransform rect, float x, float y, float width, float height)
    {
        if (rect == null)
            return;

        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(x, -y);
        rect.sizeDelta = new Vector2(width, height);
    }

    private static void SetBottomLeft(RectTransform rect, float x, float y, float width, float height)
    {
        if (rect == null)
            return;

        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.zero;
        rect.pivot = Vector2.zero;
        rect.anchoredPosition = new Vector2(x, y);
        rect.sizeDelta = new Vector2(width, height);
    }

    private static void SetBottomCenter(RectTransform rect, float x, float y, float width, float height)
    {
        if (rect == null)
            return;

        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = new Vector2(x, y);
        rect.sizeDelta = new Vector2(width, height);
    }

    private static void SetBottomRight(RectTransform rect, float x, float y, float width, float height)
    {
        if (rect == null)
            return;

        rect.anchorMin = new Vector2(1f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(1f, 0f);
        rect.anchoredPosition = new Vector2(x, y);
        rect.sizeDelta = new Vector2(width, height);
    }

    private static string BuildPath(RectTransform root, RectTransform rect)
    {
        string path = rect.name;
        Transform current = rect.parent;
        while (current != null && current != root)
        {
            path = current.name + "/" + path;
            current = current.parent;
        }

        return path;
    }
}
