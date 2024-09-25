namespace IRB_Viewer;

public partial class MainPage : ContentPage {
    private IrbFileFormat.IrbImg? imgStream;
    private int frameIndex;
    private int frameCount;

    private readonly IrDrawable irDrawable = new();
    
    public MainPage() {
        InitializeComponent();
        GraphicsView.Drawable = irDrawable;

        SetVisibilityOfImageViews(false);
    }

    private void PbOpen_OnClicked(object? sender, EventArgs e) {
        try {
            FilePickerFileType customFileType = new(
                new Dictionary<DevicePlatform, IEnumerable<string>> {
                    { DevicePlatform.WinUI, [".irb"] }, // file extension
                    { DevicePlatform.macOS, ["irb"] }, // UTType values
                });
            PickOptions options = new() {
                PickerTitle = "Please select an Infratec .irb file",
                FileTypes = customFileType
            };
            
            FilePicker.Default.PickAsync(options).ContinueWith(task => {
                if (task.Result != null) {
                    LoadIrb(task.Result.FullPath);
                }
            });
        } catch (Exception) {
            // ignored
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

        float maxValue = float.MinValue;
        float minValue = float.MaxValue;

        for (int i = 0; i < dataSize; i++) {
            maxValue = Math.Max(maxValue, img[i]);
            minValue = Math.Min(minValue, img[i]);
        }

        if (Math.Abs(maxValue - minValue) < 1) {
            maxValue = minValue + 1;
        }

        var colors = new Color[w, h];
        float scale = 255.0f / (maxValue - minValue);

        for (int i = 0; i < dataSize; i++) {
            int x = i % w;
            int y = i / w;
            int c = (int)((img[i] - minValue) * scale);
            colors[x, y] = Color.FromRgb(c, c, c);
        }

        MainThread.InvokeOnMainThreadAsync(() => {
            irDrawable.Colors = colors;
            UpdateDisplay();
        });
    }
    
    private void PbZoomIn_OnClicked(object? sender, EventArgs e) {
        GraphicsView.Scale += 0.25f;
        if (GraphicsView.Scale > 3) GraphicsView.Scale = 3;
        UpdateDisplay();
    }

    private void PbZoomOut_OnClicked(object? sender, EventArgs e) {
        GraphicsView.Scale -= 0.25f;
        if (GraphicsView.Scale < 0.5f) GraphicsView.Scale = 0.5f;
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
        LblFrameNumber.IsVisible = visible;
        LblScale.IsVisible = visible;
        
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
        LblScale.Text = $"Scale: {GraphicsView.Scale:F2}";
        
        GraphicsView.WidthRequest = irDrawable.Colors.GetLength(0) * GraphicsView.Scale;
        GraphicsView.HeightRequest = irDrawable.Colors.GetLength(1) * GraphicsView.Scale;
        GraphicsView.Invalidate();
    }

    private class IrDrawable : IDrawable {
        public Color[,]? Colors;
        public float Zoom = -1f;

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
}