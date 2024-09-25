namespace IRB_Viewer.ColorMapping;

public class ColorGradientView : GraphicsView {
    private readonly ColorGradientViewDrawable drawable = new();
    
    public ColorGradientView() {
        Drawable = drawable;
    }

    public void SetColorGradient(ColorGradient colorGradient) {
        drawable.ColorGradient = colorGradient;
        Invalidate();
    }

    public ColorGradient? GetColorGradient() {
        return drawable.ColorGradient;
    }
    
    private class ColorGradientViewDrawable : IDrawable {
        public ColorGradient? ColorGradient;
        
        public void Draw(ICanvas canvas, RectF dirtyRect) {
            if (ColorGradient is null) return;
            
            for (int x = 0; x < dirtyRect.Width; x++) {
                canvas.FillColor = ColorGradient.GetColor(ColorGradient.Min + (x / dirtyRect.Width) * (ColorGradient.Max - ColorGradient.Min));
                canvas.FillRectangle(x, 0, 1, dirtyRect.Height);
            }
        }
    }
}