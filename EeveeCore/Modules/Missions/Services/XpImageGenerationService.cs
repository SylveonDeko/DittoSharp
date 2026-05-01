using SkiaSharp;
using EeveeCore.Modules.Missions.Common;
using Serilog;

namespace EeveeCore.Modules.Missions.Services;

/// <summary>
///     Service for generating XP progress images identical to the Python implementation.
/// </summary>
public class XpImageGenerationService
{
    private const int ImageWidth = 800;
    private const int ImageHeight = 300;
    private const int ProgressBarWidth = 540;
    private const int ProgressBarHeight = 40;
    private const int AvatarSize = 120;
    private const int BorderRadius = 20;

    /// <summary>
    ///     Generates an XP progress image for a user.
    /// </summary>
    /// <param name="username">The username to display.</param>
    /// <param name="currentXp">Current XP amount.</param>
    /// <param name="level">Current level.</param>
    /// <param name="crystalSlime">Crystal slime amount.</param>
    /// <param name="title">User title.</param>
    /// <param name="avatarBytes">User avatar image bytes (optional).</param>
    /// <returns>Generated image as byte array.</returns>
    public async Task<byte[]> GenerateXpImageAsync(
        string username, 
        int currentXp, 
        int level, 
        int crystalSlime, 
        string title, 
        byte[]? avatarBytes = null)
    {
        try
        {
            using var surface = SKSurface.Create(new SKImageInfo(ImageWidth, ImageHeight));
            var canvas = surface.Canvas;
            
            canvas.Clear(new SKColor(47, 49, 54));

            var currentLevelXp = GetXpForLevel(level);
            var nextLevelXp = GetXpForLevel(level + 1);
            var xpProgress = currentXp - currentLevelXp;
            var xpNeeded = nextLevelXp - currentLevelXp;
            var progressPercentage = Math.Min((double)xpProgress / xpNeeded, 1.0);

            using var backgroundPaint = new SKPaint();
            backgroundPaint.Color = new SKColor(32, 34, 37);
            backgroundPaint.IsAntialias = true;

            var backgroundRect = new SKRoundRect(new SKRect(20, 20, ImageWidth - 20, ImageHeight - 20), BorderRadius);
            canvas.DrawRoundRect(backgroundRect, backgroundPaint);

            await DrawAvatarAsync(canvas, avatarBytes);

            DrawUsername(canvas, username);

            DrawTitle(canvas, title);

            DrawLevel(canvas, level);

            DrawProgressBar(canvas, progressPercentage, xpProgress, xpNeeded);

            DrawCrystalSlime(canvas, crystalSlime);

            DrawDecorations(canvas);

            using var image = surface.Snapshot();
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            return data.ToArray();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error generating XP image for user {Username}", username);
            throw;
        }
    }

    /// <summary>
    ///     Draws the user's avatar as a circle on the XP card, falling back to a generic placeholder when the avatar bytes are missing or fail to decode. Adds a colored stroke ring after compositing.
    /// </summary>
    /// <param name="canvas">The Skia canvas to draw onto.</param>
    /// <param name="avatarBytes">Raw avatar image bytes, or <c>null</c> if no avatar is available.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task DrawAvatarAsync(SKCanvas canvas, byte[]? avatarBytes)
    {
        await Task.CompletedTask;
        const int avatarX = 50;
        const int avatarY = (ImageHeight - AvatarSize) / 2;

        canvas.Save();

        using var clipPath = new SKPath();
        clipPath.AddCircle(avatarX + AvatarSize / 2f, avatarY + AvatarSize / 2f, AvatarSize / 2f);
        canvas.ClipPath(clipPath);

        if (avatarBytes is { Length: > 0 })
        {
            try
            {
                using var avatarBitmap = SKBitmap.Decode(avatarBytes);
                if (avatarBitmap != null)
                {
                    var avatarRect = new SKRect(avatarX, avatarY, avatarX + AvatarSize, avatarY + AvatarSize);
                    canvas.DrawBitmap(avatarBitmap, avatarRect);
                }
                else
                {
                    DrawDefaultAvatar(canvas, avatarX, avatarY);
                }
            }
            catch
            {
                DrawDefaultAvatar(canvas, avatarX, avatarY);
            }
        }
        else
        {
            DrawDefaultAvatar(canvas, avatarX, avatarY);
        }

        canvas.Restore();

        using var borderPaint = new SKPaint();
        borderPaint.Color = new SKColor(114, 137, 218);
        borderPaint.Style = SKPaintStyle.Stroke;
        borderPaint.StrokeWidth = 4;
        borderPaint.IsAntialias = true;
        canvas.DrawCircle(avatarX + AvatarSize / 2f, avatarY + AvatarSize / 2f, AvatarSize / 2f, borderPaint);
    }

    /// <summary>
    ///     Draws a stylized placeholder silhouette in the avatar slot when the user's real avatar is unavailable.
    /// </summary>
    /// <param name="canvas">The Skia canvas to draw onto.</param>
    /// <param name="x">Top-left X coordinate of the avatar region.</param>
    /// <param name="y">Top-left Y coordinate of the avatar region.</param>
    private static void DrawDefaultAvatar(SKCanvas canvas, int x, int y)
    {
        using var defaultPaint = new SKPaint();
        defaultPaint.Color = new SKColor(114, 137, 218);
        defaultPaint.IsAntialias = true;

        var rect = new SKRect(x, y, x + AvatarSize, y + AvatarSize);
        canvas.DrawRect(rect, defaultPaint);

        using var iconPaint = new SKPaint();
        iconPaint.Color = SKColors.White;
        iconPaint.IsAntialias = true;

        var centerX = x + AvatarSize / 2f;
        var centerY = y + AvatarSize / 2f;

        canvas.DrawCircle(centerX, centerY - 15, 20, iconPaint);

        var bodyRect = new SKRect(centerX - 25, centerY + 10, centerX + 25, y + AvatarSize - 10);
        canvas.DrawRect(bodyRect, iconPaint);
    }

    /// <summary>
    ///     Renders the user's display name in the header area of the XP card.
    /// </summary>
    /// <param name="canvas">The Skia canvas to draw onto.</param>
    /// <param name="username">The username to render.</param>
    private static void DrawUsername(SKCanvas canvas, string username)
    {
        using var usernamePaint = new SKPaint();
        usernamePaint.Color = SKColors.White;
        usernamePaint.IsAntialias = true;

        using var usernameFont = new SKFont(SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold), 32);

        const int usernameX = 200;
        const int usernameY = 80;
        canvas.DrawText(username, usernameX, usernameY, SKTextAlign.Left, usernameFont, usernamePaint);
    }

    /// <summary>
    ///     Renders the user's earned title beneath their username, dimming it when the user still has the default title.
    /// </summary>
    /// <param name="canvas">The Skia canvas to draw onto.</param>
    /// <param name="title">The title to render.</param>
    private static void DrawTitle(SKCanvas canvas, string title)
    {
        var titleColor = title == MissionConstants.DefaultUserTitle 
            ? new SKColor(150, 150, 150) 
            : new SKColor(128, 0, 255);

        using var titlePaint = new SKPaint();
        titlePaint.Color = titleColor;
        titlePaint.IsAntialias = true;

        using var titleFont = new SKFont(SKTypeface.FromFamilyName("Arial", SKFontStyle.Italic), 20);

        const int titleX = 200;
        const int titleY = 110;
        canvas.DrawText(title, titleX, titleY, SKTextAlign.Left, titleFont, titlePaint);
    }

    /// <summary>
    ///     Renders the user's current level in the upper-right of the XP card.
    /// </summary>
    /// <param name="canvas">The Skia canvas to draw onto.</param>
    /// <param name="level">The level number to render.</param>
    private static void DrawLevel(SKCanvas canvas, int level)
    {
        using var levelPaint = new SKPaint();
        levelPaint.Color = new SKColor(255, 215, 0);
        levelPaint.IsAntialias = true;

        using var levelFont = new SKFont(SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold), 28);

        var levelText = $"Level {level}";
        const int levelX = ImageWidth - 150;
        const int levelY = 80;
        canvas.DrawText(levelText, levelX, levelY, SKTextAlign.Left, levelFont, levelPaint);
    }

    /// <summary>
    ///     Renders the rounded XP progress bar with a gradient fill plus a centered <c>current / needed XP</c> label and shadow.
    /// </summary>
    /// <param name="canvas">The Skia canvas to draw onto.</param>
    /// <param name="progressPercentage">Fill ratio in the range [0, 1].</param>
    /// <param name="xpProgress">XP earned within the current level.</param>
    /// <param name="xpNeeded">Total XP required to reach the next level.</param>
    private static void DrawProgressBar(SKCanvas canvas, double progressPercentage, int xpProgress, int xpNeeded)
    {
        const int barX = 200;
        const int barY = 150;

        using var backgroundBarPaint = new SKPaint();
        backgroundBarPaint.Color = new SKColor(64, 68, 75);
        backgroundBarPaint.IsAntialias = true;

        var backgroundBarRect = new SKRoundRect(
            new SKRect(barX, barY, barX + ProgressBarWidth, barY + ProgressBarHeight), 
            ProgressBarHeight / 2f);
        canvas.DrawRoundRect(backgroundBarRect, backgroundBarPaint);

        var fillWidth = (float)(ProgressBarWidth * progressPercentage);
        if (fillWidth > 0)
        {
            using var progressBarPaint = new SKPaint();
            progressBarPaint.IsAntialias = true;

            var gradientColors = new[] 
            { 
                new SKColor(59, 179, 116),
                new SKColor(88, 202, 140)
            };
            
            var gradientPositions = new[] { 0f, 1f };
            var gradient = SKShader.CreateLinearGradient(
                new SKPoint(barX, barY),
                new SKPoint(barX + fillWidth, barY),
                gradientColors,
                gradientPositions,
                SKShaderTileMode.Clamp);

            progressBarPaint.Shader = gradient;

            var progressBarRect = new SKRoundRect(
                new SKRect(barX, barY, barX + fillWidth, barY + ProgressBarHeight), 
                ProgressBarHeight / 2f);
            canvas.DrawRoundRect(progressBarRect, progressBarPaint);
        }

        using var xpTextPaint = new SKPaint();
        xpTextPaint.Color = SKColors.White;
        xpTextPaint.IsAntialias = true;

        using var xpTextFont = new SKFont(SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold), 18);

        var xpText = $"{xpProgress:N0} / {xpNeeded:N0} XP";
        var textWidth = xpTextFont.MeasureText(xpText);
        
        var textX = barX + (ProgressBarWidth - textWidth) / 2f;
        var textY = barY + (ProgressBarHeight + xpTextFont.Size) / 2f;
        
        using var shadowPaint = new SKPaint();
        shadowPaint.Color = new SKColor(0, 0, 0, 128);
        shadowPaint.IsAntialias = true;
        canvas.DrawText(xpText, textX + 1f, textY + 1f, SKTextAlign.Left, xpTextFont, shadowPaint);
        
        canvas.DrawText(xpText, textX, textY, SKTextAlign.Left, xpTextFont, xpTextPaint);
    }

    /// <summary>
    ///     Renders the user's crystal slime balance below the progress bar.
    /// </summary>
    /// <param name="canvas">The Skia canvas to draw onto.</param>
    /// <param name="crystalSlime">The crystal slime quantity to render.</param>
    private static void DrawCrystalSlime(SKCanvas canvas, int crystalSlime)
    {
        using var slimePaint = new SKPaint();
        slimePaint.Color = new SKColor(138, 43, 226);
        slimePaint.IsAntialias = true;

        using var slimeFont = new SKFont(SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold), 24);

        var slimeText = $"💎 {crystalSlime:N0} Crystal Slime";
        const int slimeX = 200;
        const int slimeY = 230;
        canvas.DrawText(slimeText, slimeX, slimeY, SKTextAlign.Left, slimeFont, slimePaint);
    }

    /// <summary>
    ///     Renders decorative card flourishes (corner triangles and scattered sparkle dots) on the XP card.
    /// </summary>
    /// <param name="canvas">The Skia canvas to draw onto.</param>
    private static void DrawDecorations(SKCanvas canvas)
    {
        
        using var decorPaint = new SKPaint();
        decorPaint.Color = new SKColor(114, 137, 218, 50);
        decorPaint.IsAntialias = true;

        using var topLeftPath = new SKPath();
        topLeftPath.MoveTo(20, 20);
        topLeftPath.LineTo(80, 20);
        topLeftPath.LineTo(20, 80);
        topLeftPath.Close();
        canvas.DrawPath(topLeftPath, decorPaint);

        using var bottomRightPath = new SKPath();
        bottomRightPath.MoveTo(ImageWidth - 20, ImageHeight - 20);
        bottomRightPath.LineTo(ImageWidth - 80, ImageHeight - 20);
        bottomRightPath.LineTo(ImageWidth - 20, ImageHeight - 80);
        bottomRightPath.Close();
        canvas.DrawPath(bottomRightPath, decorPaint);

        using var sparklePaint = new SKPaint();
        sparklePaint.Color = new SKColor(255, 215, 0, 180);
        sparklePaint.IsAntialias = true;

        var sparklePositions = new[]
        {
            new SKPoint(650, 50),
            new SKPoint(720, 80),
            new SKPoint(680, 120),
            new SKPoint(150, 260),
            new SKPoint(580, 250)
        };

        foreach (var pos in sparklePositions)
        {
            canvas.DrawCircle(pos.X, pos.Y, 3, sparklePaint);
        }
    }

    /// <summary>
    ///     Returns the total XP required to reach a given level, computed as <c>BaseXp * level^LevelExponent</c>.
    /// </summary>
    /// <param name="level">The level to compute XP for.</param>
    /// <returns>The total XP required to reach <paramref name="level"/>.</returns>
    private static int GetXpForLevel(int level)
    {
        return (int)(MissionConstants.BaseXp * Math.Pow(level, MissionConstants.LevelExponent));
    }
}