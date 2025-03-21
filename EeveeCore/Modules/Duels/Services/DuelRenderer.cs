using EeveeCore.Modules.Duels.Extensions;
using EeveeCore.Modules.Duels.Impl;
using EeveeCore.Services;
using EeveeCore.Services.Impl;
using MongoDB.Driver;
using SkiaSharp;

namespace EeveeCore.Modules.Duels.Services;

public class DuelRenderer(IMongoService mongoService) : INService
{
    private const string ResourcePath = "data/";
    private readonly HttpClient _httpClient = new();
    private readonly Dictionary<string, SKBitmap> _imageCache = new();

    /// <summary>
    ///     Generate and send a team preview image
    /// </summary>
    public async Task<IUserMessage> GenerateTeamPreview(Battle? battle)
    {
        // Create embed for team preview
        var embed = new EmbedBuilder()
            .WithTitle("Pokemon Battle accepted! Loading...")
            .WithDescription("Team Preview")
            .WithColor(new Color(255, 182, 193))
            .WithFooter("Who Wins!?");

        // Generate preview image
        using var previewImage = await GenerateTeamPreviewImage(battle);
        using var memoryStream = new MemoryStream();
        previewImage.Encode(SKEncodedImageFormat.Png, 100).SaveTo(memoryStream);
        memoryStream.Position = 0;

        // Create components for selecting lead Pokémon
        var components = new ComponentBuilder()
            .WithButton("Select a lead pokemon", "battle:select_lead")
            .Build();

        // Send the message
        return await battle.Channel.SendFileAsync(
            memoryStream,
            "team_preview.png",
            embed: embed.Build(),
            components: components);
    }

    /// <summary>
    ///     Generate a team preview image
    /// </summary>
    private async Task<SKImage> GenerateTeamPreviewImage(Battle? battle)
    {
        var width = 800;
        var height = 400;

        using var surface = SKSurface.Create(new SKImageInfo(width, height));
        var canvas = surface.Canvas;

        // Fill background with Pokémon style pattern
        DrawPokemonStyleBackground(canvas, width, height);

        // Draw header
        using var titlePaint = new SKPaint
        {
            Color = SKColors.Black,
            TextSize = 32,
            IsAntialias = true,
            TextAlign = SKTextAlign.Center,
            Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyleWeight.Bold, SKFontStyleWidth.Normal,
                SKFontStyleSlant.Upright)
        };
        canvas.DrawText("Team Preview", width / 2, 40, titlePaint);

        // Draw trainer names
        using var trainerPaint = new SKPaint
        {
            Color = SKColors.Black,
            TextSize = 24,
            IsAntialias = true,
            Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyleWeight.Bold, SKFontStyleWidth.Normal,
                SKFontStyleSlant.Upright)
        };

        // Draw trainer 1 name in red
        trainerPaint.Color = new SKColor(255, 0, 0);
        canvas.DrawText(battle.Trainer1.Name, 150, 80, trainerPaint);

        // Draw trainer 2 name in blue
        trainerPaint.Color = new SKColor(0, 0, 255);
        canvas.DrawText(battle.Trainer2.Name, width - 150, 80, trainerPaint);

        // Draw dividing line
        using var linePaint = new SKPaint
        {
            Color = SKColors.Black,
            StrokeWidth = 2,
            IsAntialias = true,
            Style = SKPaintStyle.Stroke
        };
        canvas.DrawLine(width / 2, 60, width / 2, height - 20, linePaint);

        // Draw trainer 1's Pokémon
        await DrawTrainerTeam(canvas, battle.Trainer1, 50, 100, 300, 280);

        // Draw trainer 2's Pokémon
        await DrawTrainerTeam(canvas, battle.Trainer2, 450, 100, 300, 280);

        return surface.Snapshot();
    }

    /// <summary>
    ///     Draw a Pokémon style pattern background
    /// </summary>
    private void DrawPokemonStyleBackground(SKCanvas canvas, int width, int height)
    {
        // Draw gradient background similar to Pokémon games
        using var bgPaint = new SKPaint
        {
            Shader = SKShader.CreateLinearGradient(
                new SKPoint(0, 0),
                new SKPoint(0, height),
                new[] { new SKColor(226, 246, 255), new SKColor(187, 227, 255) },
                null,
                SKShaderTileMode.Clamp)
        };
        canvas.DrawRect(0, 0, width, height, bgPaint);

        // Draw a subtle pattern overlay
        using var patternPaint = new SKPaint
        {
            Color = new SKColor(255, 255, 255, 20),
            IsAntialias = true
        };

        for (var i = 0; i < width; i += 20)
        for (var j = 0; j < height; j += 20)
            canvas.DrawCircle(i, j, 1, patternPaint);
    }

    /// <summary>
    ///     Draw a trainer's team on the preview
    /// </summary>
    private async Task DrawTrainerTeam(SKCanvas canvas, Trainer? trainer, int x, int y, int width, int height)
    {
        var pokemonPerRow = 2;
        var pokemonWidth = width / pokemonPerRow;
        var pokemonHeight = height / 3;

        using var namePaint = new SKPaint
        {
            Color = SKColors.Black,
            TextSize = 18,
            IsAntialias = true,
            Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyleWeight.Normal, SKFontStyleWidth.Normal,
                SKFontStyleSlant.Upright)
        };

        using var levelPaint = new SKPaint
        {
            Color = SKColors.DarkGray,
            TextSize = 14,
            IsAntialias = true,
            Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyleWeight.Normal, SKFontStyleWidth.Normal,
                SKFontStyleSlant.Upright)
        };

        for (var i = 0; i < trainer.Party.Count; i++)
        {
            var pokemon = trainer.Party[i];
            var row = i / pokemonPerRow;
            var col = i % pokemonPerRow;

            var pokemonX = x + col * pokemonWidth;
            var pokemonY = y + row * pokemonHeight;

            // Get Pokémon sprite
            var fileName = await GetPokemonFileName(pokemon, mongoService);
            var sprite = await LoadPokemonBitmap($"pixel_sprites/{fileName}");

            if (sprite != null)
            {
                // Draw Pokémon sprite with shadow
                DrawPokemonWithShadow(canvas, sprite, pokemonX + 10, pokemonY + 5, 64, 64);

                // Draw Pokémon name and level in Pokémon style box
                DrawPokemonInfoBox(canvas, pokemon.Name, pokemon.Level, pokemonX + 80, pokemonY + 20, 180, 50);
            }
        }
    }

    /// <summary>
    ///     Draw a Pokémon sprite with shadow effect
    /// </summary>
    private void DrawPokemonWithShadow(SKCanvas canvas, SKBitmap sprite, float x, float y, float width, float height)
    {
        // Draw shadow
        using var shadowPaint = new SKPaint
        {
            Color = new SKColor(0, 0, 0, 70),
            IsAntialias = true,
            MaskFilter = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, 3)
        };
        canvas.DrawOval(new SKRect(x + 5, y + height - 10, x + width - 5, y + height), shadowPaint);

        // Draw Pokémon sprite
        var rect = new SKRect(x, y, x + width, y + height);
        canvas.DrawBitmap(sprite, rect);
    }

    /// <summary>
    ///     Draw Pokémon info box in Pokémon game style
    /// </summary>
    private void DrawPokemonInfoBox(SKCanvas canvas, string name, int level, float x, float y, float width,
        float height)
    {
        // Draw box background
        using var boxBgPaint = new SKPaint
        {
            Color = new SKColor(248, 248, 248),
            IsAntialias = true
        };

        using var boxBorderPaint = new SKPaint
        {
            Color = new SKColor(80, 80, 80),
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 2
        };

        var boxRect = new SKRoundRect(new SKRect(x, y, x + width, y + height), 10, 10);
        canvas.DrawRoundRect(boxRect, boxBgPaint);
        canvas.DrawRoundRect(boxRect, boxBorderPaint);

        // Draw Pokémon name
        using var namePaint = new SKPaint
        {
            Color = SKColors.Black,
            TextSize = 18,
            IsAntialias = true,
            Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyleWeight.Bold, SKFontStyleWidth.Normal,
                SKFontStyleSlant.Upright)
        };
        canvas.DrawText(name, x + 10, y + 25, namePaint);

        // Draw level
        using var levelPaint = new SKPaint
        {
            Color = SKColors.DarkGray,
            TextSize = 14,
            IsAntialias = true,
            Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyleWeight.Normal, SKFontStyleWidth.Normal,
                SKFontStyleSlant.Upright)
        };
        canvas.DrawText($"Lv. {level}", x + 10, y + 45, levelPaint);
    }

    /// <summary>
    ///     Generate and send a main battle message
    /// </summary>
    public async Task<IUserMessage> GenerateMainBattleMessage(Battle battle)
    {
        // Create embed for battle
        var embed = new EmbedBuilder()
            .WithTitle($"Battle between {battle.Trainer1.Name} and {battle.Trainer2.Name}")
            .WithColor(new Color(255, 182, 193))
            .WithFooter("Who Wins!?");

        // Generate battle image
        using var battleImage = await GenerateBattleImage(battle);
        using var memoryStream = new MemoryStream();
        battleImage.Encode(SKEncodedImageFormat.Png, 100).SaveTo(memoryStream);
        memoryStream.Position = 0;

        // Create components for battle actions
        var components = new ComponentBuilder()
            .WithButton("View your actions", "battle:actions")
            .Build();

        // Update battle interaction turn
        battle.SetCurrentInteractionTurn(battle.Turn);

        // Send the message
        return await battle.Channel.SendFileAsync(
            memoryStream,
            "battle.png",
            embed: embed.Build(),
            components: components);
    }

    /// <summary>
    ///     Generate a battle image
    /// </summary>
    public async Task<SKImage> GenerateBattleImage(Battle battle)
    {
        var width = 800;
        var height = 450;

        using var surface = SKSurface.Create(new SKImageInfo(width, height));
        var canvas = surface.Canvas;

        // Draw background
        await DrawBackground(canvas, battle.BgNum, width, height);

        // Draw weather effects if any
        if (battle.Weather.WeatherType is not null)
            await DrawWeatherEffect(canvas, battle.Weather.Get(), width, height);

        // Draw trick room effect if active
        if (battle.TrickRoom.Active()) DrawTrickRoomEffect(canvas, width, height);

        // Draw Pokémon
        await DrawBattlePokemon(canvas, battle, width, height);

        // Draw HP bars and info
        DrawPokemonStatus(canvas, battle, width, height);

        // No UI menu elements as requested

        return surface.Snapshot();
    }

    /// <summary>
    ///     Draw battle interface with Pokémon-style menu
    /// </summary>
    private void DrawBattleInterface(SKCanvas canvas, int width, int height)
    {
        // Draw main battle menu box
        using var menuBgPaint = new SKPaint
        {
            Color = new SKColor(248, 248, 248),
            IsAntialias = true
        };

        using var menuBorderPaint = new SKPaint
        {
            Color = SKColors.Black,
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 2
        };

        var menuRect = new SKRoundRect(new SKRect(10, height - 120, width - 10, height - 10), 15, 15);
        canvas.DrawRoundRect(menuRect, menuBgPaint);
        canvas.DrawRoundRect(menuRect, menuBorderPaint);

        // Draw the four battle options
        DrawBattleOption(canvas, "FIGHT", 30, height - 105, 180, 40);
        DrawBattleOption(canvas, "BAG", 230, height - 105, 180, 40);
        DrawBattleOption(canvas, "POKEMON", 430, height - 105, 180, 40);
        DrawBattleOption(canvas, "RUN", 630, height - 105, 140, 40);

        // Draw text area for battle messages
        using var textBgPaint = new SKPaint
        {
            Color = new SKColor(240, 240, 240),
            IsAntialias = true
        };

        var textRect = new SKRoundRect(new SKRect(30, height - 55, width - 30, height - 20), 10, 10);
        canvas.DrawRoundRect(textRect, textBgPaint);
        canvas.DrawRoundRect(textRect, menuBorderPaint);

        using var textPaint = new SKPaint
        {
            Color = SKColors.Black,
            TextSize = 16,
            IsAntialias = true,
            Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyleWeight.Normal, SKFontStyleWidth.Normal,
                SKFontStyleSlant.Upright)
        };

        canvas.DrawText("What will you do?", 50, height - 30, textPaint);
    }

    /// <summary>
    ///     Draw a battle menu option button
    /// </summary>
    private void DrawBattleOption(SKCanvas canvas, string text, float x, float y, float width, float height)
    {
        using var bgPaint = new SKPaint
        {
            Color = new SKColor(220, 220, 220),
            IsAntialias = true
        };

        using var borderPaint = new SKPaint
        {
            Color = SKColors.Black,
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 1
        };

        var rect = new SKRoundRect(new SKRect(x, y, x + width, y + height), 8, 8);
        canvas.DrawRoundRect(rect, bgPaint);
        canvas.DrawRoundRect(rect, borderPaint);

        using var textPaint = new SKPaint
        {
            Color = SKColors.Black,
            TextSize = 18,
            IsAntialias = true,
            TextAlign = SKTextAlign.Center,
            Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyleWeight.Bold, SKFontStyleWidth.Normal,
                SKFontStyleSlant.Upright)
        };

        canvas.DrawText(text, x + width / 2, y + height / 2 + 6, textPaint);
    }

    /// <summary>
    ///     Draw the battle background
    /// </summary>
    private async Task DrawBackground(SKCanvas canvas, int bgNum, int width, int height)
    {
        var bgPath = Path.Combine(ResourcePath, "backgrounds", $"bg{bgNum}.png");

        if (File.Exists(bgPath))
        {
            var background = await LoadBitmapFromFile(bgPath);
            var rect = new SKRect(0, 0, width, height);
            canvas.DrawBitmap(background, rect);
        }
        else
        {
            // Fallback to a gradient background
            using var paint = new SKPaint
            {
                Shader = SKShader.CreateLinearGradient(
                    new SKPoint(0, 0),
                    new SKPoint(0, height),
                    new[] { new SKColor(135, 206, 235), new SKColor(34, 139, 34) },
                    null,
                    SKShaderTileMode.Clamp)
            };
            canvas.DrawRect(0, 0, width, height, paint);
        }
    }

    /// <summary>
    ///     Draw weather effects
    /// </summary>
    private async Task DrawWeatherEffect(SKCanvas canvas, string? weatherType, int width, int height)
    {
        switch (weatherType)
        {
            case "rain":
                DrawRainEffect(canvas, width, height);
                break;
            case "h-sun":
                DrawSunEffect(canvas, width, height);
                break;
            case "sandstorm": // Sandstorm
                DrawSandstormEffect(canvas, width, height);
                break;
            case "hail":
                DrawHailEffect(canvas, width, height);
                break;
            case "fog":
                DrawFogEffect(canvas, width, height);
                break;
        }
    }

    /// <summary>
    ///     Draw rain effect
    /// </summary>
    private void DrawRainEffect(SKCanvas canvas, int width, int height)
    {
        var random = new Random();

        // Draw rain drops
        using var rainPaint = new SKPaint
        {
            Color = new SKColor(150, 200, 255, 180),
            IsAntialias = true,
            StrokeWidth = 2,
            Style = SKPaintStyle.Stroke
        };

        for (var i = 0; i < 100; i++)
        {
            float x1 = random.Next(width);
            float y1 = random.Next(height);
            var x2 = x1 - 10;
            var y2 = y1 + 20;

            canvas.DrawLine(x1, y1, x2, y2, rainPaint);
        }

        // Add a blue overlay
        using var overlayPaint = new SKPaint
        {
            Color = new SKColor(0, 0, 150, 30)
        };
        canvas.DrawRect(0, 0, width, height, overlayPaint);
    }

    /// <summary>
    ///     Draw sun effect
    /// </summary>
    private void DrawSunEffect(SKCanvas canvas, int width, int height)
    {
        // Draw sun
        using var sunPaint = new SKPaint();
        sunPaint.Color = new SKColor(255, 200, 0, 150);
        sunPaint.IsAntialias = true;

        canvas.DrawCircle(width / 2, height / 4, 60, sunPaint);

        // Draw sun rays
        using var rayPaint = new SKPaint
        {
            Color = new SKColor(255, 200, 0, 100),
            IsAntialias = true,
            StrokeWidth = 5
        };

        for (var i = 0; i < 12; i++)
        {
            var angle = i * 30 * Math.PI / 180;
            var x1 = width / 2 + (float)(80 * Math.Cos(angle));
            var y1 = height / 4 + (float)(80 * Math.Sin(angle));
            var x2 = width / 2 + (float)(120 * Math.Cos(angle));
            var y2 = height / 4 + (float)(120 * Math.Sin(angle));

            canvas.DrawLine(x1, y1, x2, y2, rayPaint);
        }

        // Add a yellow overlay
        using var overlayPaint = new SKPaint
        {
            Color = new SKColor(255, 200, 0, 30)
        };
        canvas.DrawRect(0, 0, width, height, overlayPaint);
    }

    /// <summary>
    ///     Draw sandstorm effect
    /// </summary>
    private void DrawSandstormEffect(SKCanvas canvas, int width, int height)
    {
        var random = new Random();

        // Draw sand particles
        using var sandPaint = new SKPaint
        {
            Color = new SKColor(210, 180, 140, 150),
            IsAntialias = true
        };

        for (var i = 0; i < 200; i++)
        {
            float x = random.Next(width);
            float y = random.Next(height);
            float size = random.Next(1, 4);

            canvas.DrawRect(x, y, x + size, y + size, sandPaint);
        }

        // Add a sand-colored overlay
        using var overlayPaint = new SKPaint
        {
            Color = new SKColor(210, 180, 140, 50)
        };
        canvas.DrawRect(0, 0, width, height, overlayPaint);
    }

    /// <summary>
    ///     Draw hail effect
    /// </summary>
    private void DrawHailEffect(SKCanvas canvas, int width, int height)
    {
        var random = new Random();

        // Draw hail particles
        using var hailPaint = new SKPaint
        {
            Color = new SKColor(220, 240, 255, 200),
            IsAntialias = true
        };

        for (var i = 0; i < 80; i++)
        {
            float x = random.Next(width);
            float y = random.Next(height);
            float size = random.Next(2, 6);

            canvas.DrawCircle(x, y, size, hailPaint);
        }

        // Add a blue-white overlay
        using var overlayPaint = new SKPaint
        {
            Color = new SKColor(200, 220, 255, 30)
        };
        canvas.DrawRect(0, 0, width, height, overlayPaint);
    }

    /// <summary>
    ///     Draw fog effect
    /// </summary>
    private void DrawFogEffect(SKCanvas canvas, int width, int height)
    {
        // Draw fog overlay
        using var fogPaint = new SKPaint
        {
            Color = new SKColor(255, 255, 255, 100)
        };
        canvas.DrawRect(0, 0, width, height, fogPaint);

        // Draw fog patches
        var random = new Random();
        using var patchPaint = new SKPaint
        {
            Color = new SKColor(255, 255, 255, 70),
            IsAntialias = true,
            MaskFilter = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, 20)
        };

        for (var i = 0; i < 15; i++)
        {
            float x = random.Next(width);
            float y = random.Next(height);
            float sizeX = random.Next(50, 150);
            float sizeY = random.Next(20, 50);

            canvas.DrawOval(new SKRect(x, y, x + sizeX, y + sizeY), patchPaint);
        }
    }

    /// <summary>
    ///     Draw trick room effect
    /// </summary>
    private void DrawTrickRoomEffect(SKCanvas canvas, int width, int height)
    {
        // Draw grid pattern
        using var gridPaint = new SKPaint
        {
            Color = new SKColor(180, 100, 220, 100),
            IsAntialias = true,
            StrokeWidth = 1,
            Style = SKPaintStyle.Stroke
        };

        // Horizontal lines
        for (var y = 0; y < height; y += 30) canvas.DrawLine(0, y, width, y, gridPaint);

        // Vertical lines
        for (var x = 0; x < width; x += 30) canvas.DrawLine(x, 0, x, height, gridPaint);

        // Add a purple tint
        using var tintPaint = new SKPaint
        {
            Color = new SKColor(180, 100, 220, 20)
        };
        canvas.DrawRect(0, 0, width, height, tintPaint);
    }

    /// <summary>
    ///     Draw the Pokémon on the battle scene
    /// </summary>
    private async Task DrawBattlePokemon(SKCanvas canvas, Battle battle, int width, int height)
    {
        // Draw player's Pokémon (left side, back view like in Pokémon games)
        if (battle.Trainer1.CurrentPokemon != null)
        {
            var pokemon = battle.Trainer1.CurrentPokemon;
            var directory = "images";

            // Determine skin or default sprite
            if (pokemon.Skin != null && !pokemon.Skin.Contains("verification")) directory = "skins";

            var fileName = await GetPokemonFileName(pokemon, mongoService);

            if (pokemon.Substitute > 0)
            {
                // Draw substitute
                var substitute = await LoadPokemonBitmap("images/substitute.png");
                if (substitute != null)
                {
                    // Larger substitute sprite
                    var rect = new SKRect(100, height - 230, 250, height - 80);
                    canvas.DrawBitmap(substitute, rect);
                }
            }
            else
            {
                // Draw Pokémon
                var sprite = await LoadPokemonBitmap($"{directory}/{fileName}");
                if (sprite != null)
                {

                    // Larger Pokémon sprite (50% bigger)
                    var rect = new SKRect(100, height - 230, 250, height - 80);
                    canvas.DrawBitmap(sprite, rect);
                }
            }
        }

        // Draw opponent's Pokémon (right side)
        if (battle.Trainer2.CurrentPokemon != null)
        {
            var pokemon = battle.Trainer2.CurrentPokemon;
            var directory = "images";

            // Determine skin or default sprite
            if (pokemon.Skin != null && !pokemon.Skin.Contains("verification")) directory = "skins";

            var fileName = await GetPokemonFileName(pokemon, mongoService);

            if (pokemon.Substitute > 0)
            {
                // Draw substitute
                var substitute = await LoadPokemonBitmap("images/substitute.png");
                if (substitute != null)
                {

                    var rect = new SKRect(width - 270, 80, width - 100, 230);
                    canvas.DrawBitmap(substitute, rect);
                }
            }
            else
            {
                // Draw Pokémon
                var sprite = await LoadPokemonBitmap($"{directory}/{fileName}");
                if (sprite != null)
                {

                    // Larger Pokémon sprite (50% bigger)
                    var rect = new SKRect(width - 270, 80, width - 100, 230);
                    canvas.DrawBitmap(sprite, rect);
                }
            }
        }
    }


    //// <summary>
    /// Draw Pokémon status and HP bars
    /// </summary>
    private void DrawPokemonStatus(SKCanvas canvas, Battle battle, int width, int height)
    {
        // Draw player's Pokémon info (bottom right) in Pokémon style - like in Image 1
        if (battle.Trainer1.CurrentPokemon != null)
        {
            var pokemon = battle.Trainer1.CurrentPokemon;
            DrawPokemonGameStyleHpBar(canvas, pokemon, 40, 40, 300, true);
        }

        // Draw opponent's Pokémon info (top right) in Pokémon style - like in Image 1
        if (battle.Trainer2.CurrentPokemon != null)
        {
            var pokemon = battle.Trainer2.CurrentPokemon;
            DrawPokemonGameStyleHpBar(canvas, pokemon, width - 340, 40, 300, false);
        }
    }

    /// <summary>
    ///     Draw Pokémon style HP bar with proper spacing
    /// </summary>
    private void DrawPokemonGameStyleHpBar(SKCanvas canvas, DuelPokemon pokemon, float x, float y, float width,
        bool isPlayer)
    {
        // Draw Pokémon-style HP info box with angled edge like in Image 1
        using var boxPath = new SKPath();
        boxPath.MoveTo(x, y);
        boxPath.LineTo(x + width, y);
        boxPath.LineTo(x + width, y + 70);
        boxPath.LineTo(x + width - 30, y + 70); // Create angled edge
        boxPath.LineTo(x, y + 70);
        boxPath.Close();

        using var boxBgPaint = new SKPaint
        {
            Color = new SKColor(220, 220, 220),
            IsAntialias = true
        };

        using var boxBorderPaint = new SKPaint
        {
            Color = new SKColor(60, 60, 60),
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 2
        };

        canvas.DrawPath(boxPath, boxBgPaint);
        canvas.DrawPath(boxPath, boxBorderPaint);

        // Draw name and gender symbol
        using var namePaint = new SKPaint
        {
            Color = SKColors.Black,
            TextSize = 22,
            IsAntialias = true,
            Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyleWeight.Bold, SKFontStyleWidth.Normal,
                SKFontStyleSlant.Upright)
        };

        canvas.DrawText(pokemon.Name, x + 15, y + 25, namePaint);

        // Draw level with gender symbol like in Image 1
        var genderSymbol = pokemon.Gender == "male" ? "♂" : pokemon.Gender == "female" ? "♀" : "";
        using var levelPaint = new SKPaint
        {
            Color = SKColors.Black,
            TextSize = 18,
            IsAntialias = true
        };

        var levelText = $"{genderSymbol}Lv.{pokemon.Level}";
        canvas.DrawText(levelText, x + width - 70, y + 25, levelPaint);

        // Draw HP label
        using var hpLabelPaint = new SKPaint
        {
            Color = SKColors.Black,
            TextSize = 18,
            IsAntialias = true,
            Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyleWeight.Bold, SKFontStyleWidth.Normal,
                SKFontStyleSlant.Upright)
        };

        canvas.DrawText("HP", x + 15, y + 50, hpLabelPaint);

        // Draw HP bar with proper spacing for numbers
        using var barBgPaint = new SKPaint
        {
            Color = new SKColor(180, 180, 180),
            IsAntialias = true
        };

        var barRect = new SKRect(x + 55, y + 42, x + width - 15, y + 54);
        canvas.DrawRect(barRect, barBgPaint);

        // Calculate HP percentage
        var hpPercent = (float)pokemon.Hp / pokemon.StartingHp;
        var hpBarWidth = (width - 70) * hpPercent;

        // Determine HP bar color
        var hpColor = hpPercent switch
        {
            > 0.5f => new SKColor(96, 192, 64), // Brighter green
            > 0.2f => new SKColor(248, 208, 48), // Yellow
            _ => new SKColor(240, 80, 48) // Red
        };

        using var hpPaint = new SKPaint
        {
            Color = hpColor,
            IsAntialias = true
        };

        // Draw the actual HP bar
        var hpRect = new SKRect(x + 55, y + 42, x + 55 + hpBarWidth, y + 54);
        canvas.DrawRect(hpRect, hpPaint);

        // Draw HP numbers with proper spacing
        if (isPlayer)
        {
            using var hpTextPaint = new SKPaint
            {
                Color = SKColors.Black,
                TextSize = 16,
                IsAntialias = true,
                TextAlign = SKTextAlign.Right
            };

            var hpText = $"{pokemon.Hp}/{pokemon.StartingHp}";
            canvas.DrawText(hpText, x + width - 15, y + 67, hpTextPaint);
        }
    }


    /// <summary>
    ///     Get the filename for a Pokémon's sprite
    /// </summary>
    private async Task<string> GetPokemonFileName(DuelPokemon pokemon, IMongoService mongo)
    {
        var identifier = await mongo.Forms
            .Find(f => f.Identifier.Equals(pokemon.PokemonName.ToLower(), StringComparison.CurrentCultureIgnoreCase))
            .FirstOrDefaultAsync();

        if (identifier == null)
            throw new ArgumentException($"Invalid name ({pokemon.PokemonName}) passed to GetPokemonFormInfo");

        var suffix = identifier.FormIdentifier;
        int pokemonId;
        var formId = 0;

        if (!string.IsNullOrEmpty(suffix) && pokemon.PokemonName.EndsWith(suffix))
        {
            formId = (int)(identifier.FormOrder - 1)!;
            var formName = pokemon.PokemonName[..^(suffix.Length + 1)];

            var pokemonIdentifier = await mongo.Forms
                .Find(f => f.Identifier.Equals(formName, StringComparison.CurrentCultureIgnoreCase))
                .FirstOrDefaultAsync();

            if (pokemonIdentifier == null)
                throw new ArgumentException($"Invalid name ({pokemon.PokemonName}) passed to GetPokemonFormInfo");

            pokemonId = pokemonIdentifier.PokemonId;
        }
        else
        {
            pokemonId = identifier.PokemonId;
        }

        var fileType = "png";
        var skinPath = "";
        if (!string.IsNullOrEmpty(pokemon.Skin))
        {
            if (pokemon.Skin.EndsWith("_gif"))
                fileType = "gif";
            skinPath = $"{pokemon.Skin}/";
        }

        // Assuming you have a radiant_placeholder_pokes collection
        var isPlaceholder = await mongo.RadiantPlaceholders
            .Find(p => p.Name.Equals(pokemon.PokemonName, StringComparison.CurrentCultureIgnoreCase))
            .FirstOrDefaultAsync();

        var radiantPath = pokemon.Radiant ? "radiant/" : "";
        if (pokemon.Radiant && isPlaceholder != null)
            return "placeholder.png";

        var shinyPath = pokemon.Shiny ? "shiny/" : "";
        var fileName = $"{radiantPath}{shinyPath}{skinPath}{pokemonId}-{formId}-.{fileType}";

        return fileName;
    }

    /// <summary>
    ///     Load a Pokémon bitmap
    /// </summary>
    private async Task<SKBitmap> LoadPokemonBitmap(string relativePath)
    {
        var fullPath = Path.Combine(ResourcePath, relativePath);

        // Check if file exists
        if (!File.Exists(fullPath))
        {
            // Try fallback
            var fallbackPath = Path.Combine(ResourcePath, "images", "unknown.png");
            if (File.Exists(fallbackPath))
                return await LoadBitmapFromFile(fallbackPath);

            // Create a placeholder
            return CreatePlaceholderBitmap();
        }

        return await LoadBitmapFromFile(fullPath);
    }

    /// <summary>
    ///     Load bitmap from file
    /// </summary>
    private async Task<SKBitmap> LoadBitmapFromFile(string path)
    {
        // Check cache first
        if (_imageCache.TryGetValue(path, out var cached))
            return cached;

        return await Task.Run(() =>
        {
            try
            {
                var bitmap = SKBitmap.Decode(path);
                _imageCache[path] = bitmap;
                return bitmap;
            }
            catch
            {
                return CreatePlaceholderBitmap();
            }
        });
    }

    /// <summary>
    ///     Create a placeholder bitmap
    /// </summary>
    private SKBitmap CreatePlaceholderBitmap()
    {
        var bitmap = new SKBitmap(64, 64);
        using var canvas = new SKCanvas(bitmap);

        // Fill with question mark
        canvas.Clear(SKColors.LightGray);

        using var paint = new SKPaint
        {
            Color = SKColors.Black,
            TextSize = 48,
            TextAlign = SKTextAlign.Center,
            IsAntialias = true
        };

        canvas.DrawText("?", 32, 48, paint);

        return bitmap;
    }
}