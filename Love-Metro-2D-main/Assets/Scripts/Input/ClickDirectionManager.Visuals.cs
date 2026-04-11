using UnityEngine;

public partial class ClickDirectionManager
{
    private void UpdateVisualFeedback()
    {
        if (_directionLine == null)
        {
            EnsureDirectionLine();
            if (_directionLine == null)
                return;
        }

        if (!_showDirectionLine || !HasClickDirection)
        {
            _directionLine.enabled = false;
            return;
        }

        Vector3 startPosition = transform.position;
        Vector3 endPosition = startPosition + new Vector3(CurrentClickDirection.x, CurrentClickDirection.y, 0f) * _lineLength;

        _directionLine.SetPosition(0, startPosition);
        _directionLine.SetPosition(1, endPosition);
        _directionLine.enabled = true;
    }

    private void EnsureDirectionLine()
    {
        if (_directionLine != null || !_showDirectionLine)
            return;

        CreateDirectionLine();
    }

    private void HideDirectionLine()
    {
        if (_directionLine != null)
            _directionLine.enabled = false;
    }

    private void CreateDirectionLine()
    {
        GameObject lineObject = new GameObject("DirectionLine");
        lineObject.transform.SetParent(transform, false);

        _directionLine = lineObject.AddComponent<LineRenderer>();

        Shader spriteShader = Shader.Find("Sprites/Default");
        if (spriteShader != null)
        {
            _directionLine.material = new Material(spriteShader);
        }
        else
        {
            Diagnostics.Warn("[ClickDirectionManager] Sprites/Default shader not found for direction line.");
        }

        _directionLine.startColor = _lineColor;
        _directionLine.endColor = _lineColor;
        _directionLine.startWidth = 0.1f;
        _directionLine.endWidth = 0.05f;
        _directionLine.positionCount = 2;
        _directionLine.useWorldSpace = true;
        _directionLine.sortingOrder = 10;

        Diagnostics.Log("[ClickDirectionManager] Created direction line.");
    }
}
