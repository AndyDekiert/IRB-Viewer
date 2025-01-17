﻿using System.Diagnostics.CodeAnalysis;
using CommunityToolkit.Maui.Storage;
using IRB_Viewer.ColorMapping;

namespace IRB_Viewer;

public partial class MainPage {
    private IrbFileFormat.IrbImg? imgStream;
    
    private string? filePath;
    private int frameIndex;
    private int frameCount;
    private float minValue;
    private float maxValue;
    
    private float scaleMinValue = -1;
    private float scaleMaxValue = -1;

    private readonly IrDrawable irDrawable = new();

    private const int STEPS = 1023;
    private readonly Dictionary<string, ColorGradient> colorGradients = new([
        new KeyValuePair<string, ColorGradient>("Black / White", new ColorGradient([
            new ColorGradient.ColorStop(0, Colors.Black),
            new ColorGradient.ColorStop(STEPS, Colors.White)
        ])),
        new KeyValuePair<string, ColorGradient>("Blue / Red", new ColorGradient([
            new ColorGradient.ColorStop(0, Color.FromRgb(0, 0, 80)),
            new ColorGradient.ColorStop(0.02 * STEPS, Color.FromRgb(0, 1, 128)),
            new ColorGradient.ColorStop(0.1 * STEPS, Color.FromRgb(0, 44, 150)),
            new ColorGradient.ColorStop(0.3 * STEPS, Color.FromRgb(2, 145, 200)),
            new ColorGradient.ColorStop(0.5 * STEPS, Colors.White),
            new ColorGradient.ColorStop(0.7 * STEPS, Color.FromRgb(252, 195, 12)),
            new ColorGradient.ColorStop(STEPS, Color.FromRgb(213, 3, 0))
        ])),
        new KeyValuePair<string, ColorGradient>("Rainbow", new ColorGradient([
            new ColorGradient.ColorStop(0, Color.FromRgb(0, 0, 11)),
            new ColorGradient.ColorStop(0.1 * STEPS, Color.FromRgb(0, 32, 136)),
            new ColorGradient.ColorStop(0.2 * STEPS, Color.FromRgb(0, 67, 238)),
            new ColorGradient.ColorStop(0.3 * STEPS, Color.FromRgb(0, 131, 178)),
            new ColorGradient.ColorStop(0.4 * STEPS, Color.FromRgb(0, 225, 0)),
            new ColorGradient.ColorStop(0.5 * STEPS, Color.FromRgb(215, 252, 0)),
            new ColorGradient.ColorStop(0.6 * STEPS, Color.FromRgb(252, 176, 0)),
            new ColorGradient.ColorStop(0.7 * STEPS, Color.FromRgb(252, 63, 0)),
            new ColorGradient.ColorStop(0.8 * STEPS, Color.FromRgb(252, 0, 114)),
            new ColorGradient.ColorStop(0.9 * STEPS, Color.FromRgb(252, 16, 252)),
            new ColorGradient.ColorStop(STEPS, Color.FromRgb(252, 242, 252))
        ])),
        new KeyValuePair<string, ColorGradient>("Black / White / Rainbow", new ColorGradient([
            new ColorGradient.ColorStop(0, Colors.Black),
            new ColorGradient.ColorStop(0.5 * STEPS, Colors.White),
            new ColorGradient.ColorStop((0.5 + 0.125/2) * STEPS, Color.FromRgb(0, 67, 238)),
            new ColorGradient.ColorStop((0.5 + 0.25/2) * STEPS, Color.FromRgb(0, 131, 178)),
            new ColorGradient.ColorStop((0.5 + 0.375/2) * STEPS, Color.FromRgb(0, 225, 0)),
            new ColorGradient.ColorStop((0.5 + 0.5/2) * STEPS, Color.FromRgb(215, 252, 0)),
            new ColorGradient.ColorStop((0.5 + 0.625/2) * STEPS, Color.FromRgb(252, 176, 0)),
            new ColorGradient.ColorStop((0.5 + 0.75/2) * STEPS, Color.FromRgb(252, 63, 0)),
            new ColorGradient.ColorStop((0.5 + 0.875/2) * STEPS, Color.FromRgb(252, 0, 114)),
            new ColorGradient.ColorStop(STEPS, Color.FromRgb(252, 16, 252))
        ]))
    ]);
    
    public MainPage() {
        InitializeComponent();
        GraphicsView.Drawable = irDrawable;
        
        ColorGradientView.SetColorGradient(colorGradients.First().Value);
        PickerGradient.ItemsSource = colorGradients.Keys.ToList();
        PickerGradient.SelectedIndex = 0;
        
        SetVisibilityOfImageViews(false);
    }

    private void PbOpen_OnClicked(object? sender, EventArgs e) {
        try {
            FilePickerFileType customFileType = new(
                new Dictionary<DevicePlatform, IEnumerable<string>> {
                    { DevicePlatform.WinUI, [".irb"] },
                    { DevicePlatform.macOS, ["irb"] }
                });
            PickOptions options = new() {
                PickerTitle = "Please select an Infratec .irb file",
                FileTypes = customFileType
            };
            
            FilePicker.Default.PickAsync(options).ContinueWith(task => {
                if (task.Result != null) {
                    filePath = task.Result.FullPath;
                    LoadIrb(task.Result.FullPath);
                }
            });
        } catch (Exception) {
            // ignored
        }
    }
    
    private async void PbSave_OnClicked(object? sender, EventArgs e) {
        IScreenshotResult? screenshotResult = await GraphicsView.CaptureAsync();
        if (screenshotResult != null) {
            Stream stream = await screenshotResult.OpenReadAsync();
            await FileSaver.Default.SaveAsync(Path.GetFileNameWithoutExtension(filePath) + ".png", stream);
        }
    }

    private void LoadIrb(string filepath) {
        IrbFileFormat.IrbFileReader reader = new(filepath);
        imgStream = new IrbFileFormat.IrbImg(reader);
        frameIndex = 0;
        frameCount = reader.GetImageCount();
        
        LoadFrame();
    }

    private void LoadFrame() {
        if (imgStream == null) return;
        
        if (!imgStream.ReadImage(frameIndex)) {
            Console.Error.WriteLine("EOF");
            return;
        }
        
        float[] img = imgStream.GetData();
        int w = imgStream.GetWidth();
        int h = imgStream.GetHeight();
        int dataSize = w * h;

        maxValue = float.MinValue;
        minValue = float.MaxValue;

        for (int i = 0; i < dataSize; i++) {
            maxValue = Math.Max(maxValue, img[i]);
            minValue = Math.Min(minValue, img[i]);
        }

        float mapMax = scaleMaxValue < 0 ? maxValue : scaleMaxValue;
        float mapMin = scaleMinValue < 0 ? minValue : scaleMinValue;
        if (Math.Abs(mapMax - mapMin) < 1) {
            mapMax = mapMin + 1;
        }

        var colors = new Color[w, h];
        float scale = STEPS / (mapMax - mapMin);

        for (int i = 0; i < dataSize; i++) {
            int x = i % w;
            int y = i / w;
            int c = Math.Max(0, Math.Min(STEPS, (int)((img[i] - mapMin) * scale)));
            colors[x, y] = ColorGradientView.GetColorGradient()!.GetColor(c);
        }

        MainThread.InvokeOnMainThreadAsync(() => {
            irDrawable.Colors = colors;
            UpdateDisplay();
        });
    }
    
    private void PbZoomIn_OnClicked(object? sender, EventArgs e) {
        irDrawable.Zoom += 0.25f;
        if (irDrawable.Zoom > 3) irDrawable.Zoom = 3;
        UpdateDisplay();
    }

    private void PbZoomOut_OnClicked(object? sender, EventArgs e) {
        irDrawable.Zoom -= 0.25f;
        if (irDrawable.Zoom < 0.5f) irDrawable.Zoom = 0.5f;
        UpdateDisplay();
    }
    
    private void PbGotoFirst_OnClicked(object? sender, EventArgs e) {
        frameIndex = 0;
        LoadFrame();
    }

    private void PbGotoLast_OnClicked(object? sender, EventArgs e) {
        frameIndex = frameCount - 1;
        LoadFrame();
    }

    private void PbGotoPrevious_OnClicked(object? sender, EventArgs e) {
        frameIndex--;
        if (frameIndex < 0) frameCount = 0;
        LoadFrame();
    }

    private void PbGotoNext_OnClicked(object? sender, EventArgs e) {
        frameIndex++;
        if (frameIndex >= frameCount) frameIndex = frameCount - 1;
        LoadFrame();
    }

    private void SetVisibilityOfImageViews(bool visible) {
        CtGradient.IsVisible = visible;
        
        LblFrameNumber.IsVisible = visible;
        LblScale.IsVisible = visible;
        LblRange.IsVisible = visible;
        
        PbSave.IsVisible = visible;
        PbZoomIn.IsVisible = visible;
        PbZoomOut.IsVisible = visible;
        
        PbGotoFirst.IsVisible = visible;
        PbGotoPrevious.IsVisible = visible;
        PbGotoNext.IsVisible = visible;
        PbGotoLast.IsVisible = visible;
    }

    private void UpdateDisplay() {
        if (irDrawable.Colors == null) {
            SetVisibilityOfImageViews(false);
            return;
        }

        SetVisibilityOfImageViews(true);
        LblFrameNumber.Text = $"Frame: {frameIndex + 1}/{frameCount}";
        LblScale.Text = $"Scale: {irDrawable.Zoom:F2}";
        LblRange.Text = $"Range: {minValue:F2} - {maxValue:F2} K";
        
        TxtMinValue.Placeholder = minValue.ToString("F2");
        TxtMaxValue.Placeholder = maxValue.ToString("F2");
        
        GraphicsView.WidthRequest = irDrawable.Colors.GetLength(0) * irDrawable.Zoom;
        GraphicsView.HeightRequest = irDrawable.Colors.GetLength(1) * irDrawable.Zoom;
        GraphicsView.Invalidate();
    }

    private class IrDrawable : IDrawable {
        public Color[,]? Colors;
        public float Zoom = 1.5f;

        public void Draw(ICanvas canvas, RectF dirtyRect) {
            if (Colors == null) return;
            
            for (int x = 0; x < Colors.GetLength(0); x++) {
                for (int y = 0; y < Colors.GetLength(1); y++) {
                    canvas.FillColor = Colors[x, y];
                    if (Zoom > 0) canvas.FillRectangle(GetRect(x, y));
                    else canvas.FillRectangle(x, y, 1, 1);
                }
            }
        }

        private RectF GetRect(int x, int y) {
            float fx = (float) Math.Round(x * Zoom);
            float fy = (float) Math.Round(y * Zoom);
            return new RectF(fx, fy,
                (float) Math.Round((x + 1) * Zoom) - fx,
                (float) Math.Round((y + 1) * Zoom) - fy);
        }
    }

    private void TxtMinValue_OnCompleted(object? sender, EventArgs e) {
        UpdateScaleValues();
    }

    private void TxtMaxValue_OnCompleted(object? sender, EventArgs e) {
        UpdateScaleValues();
    }

    [SuppressMessage("ReSharper", "ConditionalAccessQualifierIsNonNullableAccordingToAPIContract")]
    private void UpdateScaleValues() {
        if (TxtMinValue.Text?.Length > 0 && float.TryParse(TxtMinValue.Text, out float min)) {
            scaleMinValue = min;
        } else {
            TxtMinValue.Text = "";
            scaleMinValue = -1;
        }
        
        if (TxtMaxValue.Text?.Length > 0 && float.TryParse(TxtMaxValue.Text, out float max)) {
            scaleMaxValue = max;
        } else {
            TxtMaxValue.Text = "";
            scaleMaxValue = -1;
        }
        
        LoadFrame();
    }

    private void PickerGradient_OnSelectedIndexChanged(object? sender, EventArgs e) {
        ColorGradientView.SetColorGradient(colorGradients[(PickerGradient.SelectedItem as string)!]);
        LoadFrame();
    }
}