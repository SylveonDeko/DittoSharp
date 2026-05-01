using SkiaSharp;
using SkiaSharp.HarfBuzz;
using System.Globalization;
using HarfBuzzSharp;
using Buffer = HarfBuzzSharp.Buffer;

namespace EeveeCore.Modules.Duels.Services;

/// <summary>
///     Provides proper Unicode text rendering with HarfBuzz support for complex text,
///     emojis, ligatures, and font fallback functionality.
///     Based on the techniques described in the SkiaSharp and HarfBuzz blog post.
///     https://www.mrumpler.at/the-trouble-with-text-rendering-in-skiasharp-and-harfbuzz/
/// </summary>
public static class UnicodeTextRenderer
{
    
    /// <summary>
    ///     Draws text with proper Unicode support, font fallback, and HarfBuzz shaping.
    ///     Handles emojis, complex scripts, ligatures, and grapheme clusters correctly.
    /// </summary>
    /// <param name="canvas">The canvas to draw on.</param>
    /// <param name="x">The X position to start drawing.</param>
    /// <param name="y">The Y position (baseline) to draw at.</param>
    /// <param name="text">The text to draw.</param>
    /// <param name="font">The primary font to use.</param>
    /// <param name="paint">The paint for styling the text.</param>
    /// <param name="useHarfBuzz">Whether to use HarfBuzz shaping (slower but more accurate).</param>
    /// <returns>The total width of the rendered text.</returns>
    public static float DrawText(SKCanvas canvas, float x, float y, string text, SKFont font, SKPaint paint, bool useHarfBuzz = true)
    {
        if (string.IsNullOrEmpty(text))
            return 0;

        if (!useHarfBuzz && IsSimpleAscii(text))
        {
            return DrawTextNoFallback(canvas, x, y, text, font, paint, false);
        }

        float width = 0;

        if (font.ContainsGlyphs(text))
        {
            return DrawTextNoFallback(canvas, x, y, text, font, paint, useHarfBuzz);
        }

        var start = 0;
        var enumerator = StringInfo.GetTextElementEnumerator(text);
        
        while (enumerator.MoveNext())
        {
            var textElement = enumerator.GetTextElement();
            if (font.ContainsGlyphs(textElement)) continue;
            if (start != enumerator.ElementIndex)
            {
                var regularText = text.Substring(start, enumerator.ElementIndex - start);
                width += DrawTextNoFallback(canvas, x + width, y, regularText, font, paint, useHarfBuzz);
                start = enumerator.ElementIndex;
            }

            var foundNextGood = false;
            while (enumerator.MoveNext())
            {
                if (!font.ContainsGlyphs(enumerator.GetTextElement())) continue;
                foundNextGood = true;
                break;
            }

            var subtext = foundNextGood
                ? text.Substring(start, enumerator.ElementIndex - start)
                : text[start..];

            var firstCodepoint = subtext.EnumerateRunes().First().Value;

            var fallback = SKFontManager.Default.MatchCharacter(
                font.Typeface.FamilyName,
                font.Typeface.FontStyle,
                null,
                firstCodepoint);

            if (fallback is null)
            {
                width += DrawTextNoFallback(canvas, x + width, y, subtext, font, paint, useHarfBuzz);
            }
            else
            {
                width += DrawText(canvas, x + width, y, subtext, fallback.ToFont(font.Size), paint, useHarfBuzz);
            }

            start = foundNextGood ? enumerator.ElementIndex : text.Length;
        }

        if (start < text.Length)
        {
            width += DrawTextNoFallback(canvas, x + width, y, text[start..], font, paint, useHarfBuzz);
        }

        return width;
    }

    /// <summary>
    ///     Measures the width of text with proper Unicode support and font fallback.
    /// </summary>
    /// <param name="text">The text to measure.</param>
    /// <param name="font">The primary font to use.</param>
    /// <param name="useHarfBuzz">Whether to use HarfBuzz shaping for measurement.</param>
    /// <returns>The total width of the text.</returns>
    public static float MeasureText(string text, SKFont font, bool useHarfBuzz = true)
    {
        if (string.IsNullOrEmpty(text))
            return 0;

        if (!useHarfBuzz && IsSimpleAscii(text))
        {
            return font.MeasureText(text);
        }

        if (font.ContainsGlyphs(text))
        {
            return useHarfBuzz ? HarfBuzzMeasure(text, font) : font.MeasureText(text);
        }

        float width = 0;
        var start = 0;
        var enumerator = StringInfo.GetTextElementEnumerator(text);
        
        while (enumerator.MoveNext())
        {
            var textElement = enumerator.GetTextElement();
            if (font.ContainsGlyphs(textElement)) continue;
            if (start != enumerator.ElementIndex)
            {
                var regularText = text.Substring(start, enumerator.ElementIndex - start);
                width += useHarfBuzz ? HarfBuzzMeasure(regularText, font) : font.MeasureText(regularText);
                start = enumerator.ElementIndex;
            }

            var foundNextGood = false;
            while (enumerator.MoveNext())
            {
                if (!font.ContainsGlyphs(enumerator.GetTextElement())) continue;
                foundNextGood = true;
                break;
            }

            var subtext = foundNextGood
                ? text.Substring(start, enumerator.ElementIndex - start)
                : text[start..];

            var firstCodepoint = subtext.EnumerateRunes().First().Value;

            var fallback = SKFontManager.Default.MatchCharacter(
                font.Typeface.FamilyName,
                font.Typeface.FontStyle,
                null,
                firstCodepoint);

            if (fallback is null)
            {
                width += useHarfBuzz ? HarfBuzzMeasure(subtext, font) : font.MeasureText(subtext);
            }
            else
            {
                width += MeasureText(subtext, fallback.ToFont(font.Size), useHarfBuzz);
            }

            start = foundNextGood ? enumerator.ElementIndex : text.Length;
        }

        if (start >= text.Length) return width;
        var remainingText = text[start..];
        width += useHarfBuzz ? HarfBuzzMeasure(remainingText, font) : font.MeasureText(remainingText);

        return width;
    }

    /// <summary>
    ///     Draws text without font fallback using either SkiaSharp or HarfBuzz.
    /// </summary>
    /// <param name="canvas">The canvas to draw on.</param>
    /// <param name="x">The X position to start drawing.</param>
    /// <param name="y">The Y position (baseline) to draw at.</param>
    /// <param name="text">The text to draw.</param>
    /// <param name="font">The font to use.</param>
    /// <param name="paint">The paint for styling the text.</param>
    /// <param name="useHarfBuzz">Whether to use HarfBuzz shaping.</param>
    /// <returns>The width of the rendered text.</returns>
    private static float DrawTextNoFallback(SKCanvas canvas, float x, float y, string text, SKFont font, SKPaint paint, bool useHarfBuzz)
    {
        if (useHarfBuzz)
        {
            var width = HarfBuzzMeasure(text, font);
            canvas.DrawShapedText(text, x, y, font, paint);
            return width;
        }
        else
        {
            var width = font.MeasureText(text);
            canvas.DrawText(text, x, y, font, paint);
            return width;
        }
    }

    /// <summary>
    ///     Measures text width using HarfBuzz for accurate text shaping.
    /// </summary>
    /// <param name="text">The text to measure.</param>
    /// <param name="font">The font to use for measurement.</param>
    /// <returns>The width of the text when rendered with HarfBuzz.</returns>
    private static float HarfBuzzMeasure(string text, SKFont font)
    {
        try
        {

            using var blob = font.Typeface.OpenStream().ToHarfBuzzBlob();
            using var hbface = new Face(blob, 0);
            hbface.UnitsPerEm = font.Typeface.UnitsPerEm;
            using var hbFont = new Font(hbface);
            using var buffer = new Buffer();
            
            buffer.AddUtf16(text);
            buffer.GuessSegmentProperties();
            hbFont.Shape(buffer);

            hbFont.GetScale(out var xScale, out _);
            var scale = font.Size / xScale;
            return buffer.GlyphPositions.Sum(x => x.XAdvance) * scale;
        }
        catch
        {
            return font.MeasureText(text);
        }
    }

    /// <summary>
    ///     Checks if text contains only simple ASCII characters that don't need complex processing.
    /// </summary>
    /// <param name="text">The text to check.</param>
    /// <returns>True if the text is simple ASCII, false otherwise.</returns>
    private static bool IsSimpleAscii(string text)
    {
        return text.All(c => c <= 127);
    }
}