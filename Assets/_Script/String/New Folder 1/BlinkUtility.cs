using UnityEngine;

public static class BlinkUtility
{
    /// <summary>
    /// Áp dụng hiệu ứng nhấp nháy màu tạm thời bằng MaterialPropertyBlock.
    /// </summary>
    /// <param name="renderer">Renderer mục tiêu (LineRenderer, MeshRenderer, SpriteRenderer...)</param>
    /// <param name="defaultColor">Màu gốc khi không nhấp nháy</param>
    /// <param name="isBlink">Bật / tắt hiệu ứng nhấp nháy</param>
    /// <param name="blinkColorA">Màu nhấp nháy 1</param>
    /// <param name="blinkColorB">Màu nhấp nháy 2</param>
    /// <param name="blinkSpeed">Tốc độ nhấp nháy (dao động theo thời gian)</param>
    public static void ApplyBlinkColor(Renderer renderer, Color defaultColor, bool isBlink, Color blinkColorA, Color blinkColorB, float blinkSpeed)
    {
        if (renderer == null) return;

        MaterialPropertyBlock block = new MaterialPropertyBlock();
        renderer.GetPropertyBlock(block);

        Color finalColor;

        if (isBlink)
        {
            float t = Mathf.PingPong(Time.time * blinkSpeed, 1f);
            finalColor = Color.Lerp(blinkColorA, blinkColorB, t);
        }
        else
        {
            finalColor = defaultColor;
        }

        // Ưu tiên các property shader phổ biến
        if (renderer.sharedMaterial.HasProperty("_BaseColor"))
            block.SetColor("_BaseColor", finalColor);
        else if (renderer.sharedMaterial.HasProperty("_Color"))
            block.SetColor("_Color", finalColor);
        else if (renderer.sharedMaterial.HasProperty("_TintColor"))
            block.SetColor("_TintColor", finalColor);

        renderer.SetPropertyBlock(block);
    }
}