using System;
using Xamarin.Forms;
using SkiaSharp.Views.Forms;
using SkiaSharp;
using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Prediction.Models;
using Humanizer;
using System.Linq;

namespace ObjectDetector
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();

            var vm = (MainViewModel)BindingContext;
            vm.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(MainViewModel.Image))
                    ImageCanvas.InvalidateSurface();
            };
        }

        public void OnCanvasViewPaintSurface(object sender, SKPaintSurfaceEventArgs args)
        {
            var vm = (MainViewModel)BindingContext;
            if (vm.Image == null) return;

            var info = args.Info;
            var canvas = args.Surface.Canvas;

            ClearCanvas(info, canvas);

            var scale = Math.Min((float)info.Width / (float)vm.Image.Width, (float)info.Height / (float)vm.Image.Height);

            var scaleHeight = scale * vm.Image.Height;
            var scaleWidth = scale * vm.Image.Width;

            var top = (info.Height - scaleHeight) / 2;
            var left = (info.Width - scaleWidth) / 2;
            var right = left + scaleWidth;
            var bottom = top + scaleHeight;

            var rect = new SKRect(left, top, right, bottom);

            canvas.DrawBitmap(vm.Image, rect);

            if (vm.Predictions.All(p => p.BoundingBox != null))
            {
                // draw bounding rectangles
                foreach (var prediction in vm.Predictions)
                {
                    DrawBoundingBox(canvas, left, top, scaleWidth, scaleHeight, prediction.TagName, prediction.BoundingBox);
                }
            }
            else
            {
                var best = vm.Predictions.OrderByDescending(p => p.Probability).First();
                DrawBoundingBox(canvas, left, top, scaleWidth, scaleHeight, best.TagName, new BoundingBox(0, 0, 1, 1));
            }
        }

        static void ClearCanvas(SKImageInfo info, SKCanvas canvas)
        {
            var paint = new SKPaint
            {
                Style = SKPaintStyle.Fill,
                Color = SKColors.Black
            };

            canvas.DrawRect(info.Rect, paint);
        }

        static void DrawBoundingBox(SKCanvas canvas, float left, float top, float width, float height, string tag, BoundingBox box)
        {
            const float radius = 2.0f;
            const float xDrop = 2.0f;
            const float yDrop = 2.0f;

            var path = new SKPath();

            var scaledBoxLeft = width * (float)box.Left;
            var scaledBoxWidth = width * (float)box.Width;
            var scaledBoxTop = height * (float)box.Top;
            var scaledBoxHeight = height * (float)box.Height;

            var startLeft = left + scaledBoxLeft;
            var startTop = top + scaledBoxTop;

            path.MoveTo(startLeft, startTop);

            path.LineTo(startLeft + scaledBoxWidth, startTop);
            path.LineTo(startLeft + scaledBoxWidth, startTop + scaledBoxHeight);
            path.LineTo(startLeft, startTop + scaledBoxHeight);
            path.LineTo(startLeft, startTop);

            var strokePaint = new SKPaint
            {
                IsAntialias = true,
                Style = SKPaintStyle.Stroke,
                Color = SKColors.White,
                StrokeWidth = 5,
                PathEffect = SKPathEffect.CreateDash(new[] { 20f, 20f }, 20f)
            };

            var blurStrokePaint = new SKPaint
            {
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 5,
                PathEffect = SKPathEffect.CreateDash(new[] { 20f, 20f }, 20f),
                IsAntialias = true,
                MaskFilter = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, 0.57735f * radius + 0.5f)
            };

            canvas.DrawPath(path, blurStrokePaint);
            canvas.DrawPath(path, strokePaint);

            var textPaint = new SKPaint
            {
                IsAntialias = true,
                Color = SKColors.White
            };

            var text = tag.Humanize();

            var textWidth = textPaint.MeasureText(text);
            textPaint.TextSize = 0.9f * scaledBoxWidth * textPaint.TextSize / textWidth;

            // Find the text bounds
            var textBounds = new SKRect();
            textPaint.MeasureText(text, ref textBounds);

            var xText = (startLeft + (scaledBoxWidth / 2)) - textBounds.MidX;
            var yText = (startTop + (scaledBoxHeight / 2)) + textBounds.MidY;

            var blurPaint = new SKPaint
            {
                TextSize = textPaint.TextSize,
                IsAntialias = true,
                MaskFilter = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, 0.57735f * radius + 0.5f)
            };

            canvas.DrawText(text,
                            xText + xDrop,
                            yText + yDrop,
                            blurPaint);

            canvas.DrawText(text,
                            xText,
                            yText,
                            textPaint);
        }
    }

    internal class Sfloat
    {
    }
}
