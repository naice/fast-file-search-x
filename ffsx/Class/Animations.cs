using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using LinqToVisualTree;
using System.Linq;
using System.Windows.Controls;

namespace ffsx.Class
{
    public static class Animations
    {
        // OPACITY
        public const string propertyPathOpacity = "UIElement.Opacity";
        // FILL
        public const string propertyPathFill = "(UIElement.Fill).(SolidColorBrush.Color)";
        // BACKGROUND
        public const string propertyPathBackground = "(UIElement.Background).(SolidColorBrush.Color)";

        // TRANSFORMS
        public const string propertyPathScaleX = "(UIElement.RenderTransform).(TransformGroup.Children)[0].(ScaleTransform.ScaleX)";
        public const string propertyPathScaleY = "(UIElement.RenderTransform).(TransformGroup.Children)[0].(ScaleTransform.ScaleY)";
        public const string propertyPathTranslateX = "(UIElement.RenderTransform).(TransformGroup.Children)[1].(TranslateTransform.X)";
        public const string propertyPathTranslateY = "(UIElement.RenderTransform).(TransformGroup.Children)[1].(TranslateTransform.Y)";
        public const string propertyPathRotateAngle = "(UIElement.RenderTransform).(TransformGroup.Children)[2].(RotateTransform.Angle)";

        // PLANE PROJECTIONS
        public const string propertyPathRotationY = "(UIElement.Projection).(PlaneProjection.RotationY)";
        public const string propertyPathRotationX = "(UIElement.Projection).(PlaneProjection.RotationX)";
        public const string propertyPathRotationZ = "(UIElement.Projection).(PlaneProjection.RotationZ)";

        const double TAP_SCALE = 0.9;
        const double HOLD_SCALE = 1.1;
        const double TICKER_FLYIN_DURATION = 0.75;
        const double TICKER_FLYIN_OFFSET = 90;
        const double TICKER_SCALE_BEGIN = 1.2;
        const double TICKER_ROTATE_ANGLE = 15;
        const double TICKER_DOWNSCALE = 0.9;

        public const int TRANSFORM_SCALE = 0;
        public const int TRANSFORM_TRANSLATE = 1;
        public const int TRANSFORM_ROTATE = 2;


        public static bool makeTransformGroup(FrameworkElement frameworkElement, ScaleTransform scale = null, TranslateTransform translate = null, RotateTransform rotate = null)
        {
            if (!(frameworkElement.RenderTransform is TransformGroup))
            {
                if (frameworkElement.RenderTransformOrigin == null)
                    frameworkElement.RenderTransformOrigin = new Point(0.5, 0.5);
                TransformGroup group = new TransformGroup();
                group.Children.Add(scale == null ? new ScaleTransform() : scale);
                group.Children.Add(translate == null ? new TranslateTransform() : translate);
                group.Children.Add(rotate == null ? new RotateTransform() : rotate);
                frameworkElement.RenderTransform = group;
                return true;
            }

            return false;
        }


        public static void FrameworkElementTap(FrameworkElement frameworkElement, double downScale = TAP_SCALE)
        {
            if (frameworkElement == null) return;

            makeTransformGroup(frameworkElement);

            Storyboard sb = new Storyboard();
            // Scale - X
            DoubleAnimation anim = new DoubleAnimation();
            anim.Duration = TimeSpan.FromSeconds(0.3);
            anim.EasingFunction = new CubicEase() { EasingMode = EasingMode.EaseOut, };
            anim.From = 1;
            anim.To = downScale;
            anim.AutoReverse = true;
            Storyboard.SetTarget(anim, frameworkElement);
            Storyboard.SetTargetProperty(anim, new PropertyPath(propertyPathScaleX));
            sb.Children.Add(anim);

            // Scale - Y
            anim = new DoubleAnimation();
            anim.Duration = TimeSpan.FromSeconds(0.3);
            anim.EasingFunction = new CubicEase() { EasingMode = EasingMode.EaseOut, };
            anim.From = 1;
            anim.To = downScale;
            anim.AutoReverse = true;
            Storyboard.SetTarget(anim, frameworkElement);
            Storyboard.SetTargetProperty(anim, new PropertyPath(propertyPathScaleY));
            sb.Children.Add(anim);


            sb.Begin();
        }

        /// <summary>
        /// Requires frameworkElement to have a Rectangle Child with Name="touchAnimationRect"
        /// </summary>
        public static void FrameworkElementDown(FrameworkElement frameworkElement, Color defaultColor, Color highlightColor)
        {
            if (frameworkElement == null) return;
            Rectangle rect = frameworkElement.GetChildren<Rectangle>().FirstOrDefault((A) => A.Name == "touchAnimationRect");
            if (rect == null) return;
            if (rect.Fill == null) rect.Fill = new SolidColorBrush(defaultColor);

            Storyboard sb = new Storyboard();
            // Fill
            ColorAnimation anim = new ColorAnimation();
            anim.Duration = TimeSpan.FromSeconds(0.4);
            anim.EasingFunction = new CubicEase() { EasingMode = EasingMode.EaseInOut, };
            anim.To = highlightColor;
            anim.AutoReverse = false;
            Storyboard.SetTarget(anim, rect);
            Storyboard.SetTargetProperty(anim, new PropertyPath(propertyPathFill));
            sb.Children.Add(anim);


            sb.Begin();
        }
        /// <summary>
        /// Requires frameworkElement to have a Rectangle Child with Name="touchAnimationRect"
        /// </summary>
        public static void FrameworkElementUp(FrameworkElement frameworkElement, Color defaultColor)
        {
            if (frameworkElement == null) return;
            Rectangle rect = frameworkElement.GetChildren<Rectangle>().FirstOrDefault((A) => A.Name == "touchAnimationRect");
            if (rect == null) return;
            if (rect.Fill == null) rect.Fill = new SolidColorBrush(defaultColor);

            Storyboard sb = new Storyboard();
            // Fill
            ColorAnimation anim = new ColorAnimation();
            anim.Duration = TimeSpan.FromSeconds(0.4);
            anim.EasingFunction = new CubicEase() { EasingMode = EasingMode.EaseInOut, };
            anim.To = defaultColor;
            anim.AutoReverse = false;
            Storyboard.SetTarget(anim, rect);
            Storyboard.SetTargetProperty(anim, new PropertyPath(propertyPathFill));
            sb.Children.Add(anim);


            sb.Begin();
        }

        public static void FrameworkElementHold(Panel panel, Color defaultColor, Color highlightColor, Action inAction = null, Action releaseAction = null)
        {
            if (panel == null) return;
            if (panel.Background == null) panel.Background = new SolidColorBrush(defaultColor);

            System.Windows.Input.MouseEventHandler mouseLeave = null;
            mouseLeave = new System.Windows.Input.MouseEventHandler((sender, args) =>
            {
                Storyboard revStoryboard = new Storyboard();
                // Background
                ColorAnimation revAnim = new ColorAnimation();
                revAnim.Duration = TimeSpan.FromSeconds(0.4);
                revAnim.EasingFunction = new CubicEase() { EasingMode = EasingMode.EaseInOut, };
                revAnim.To = defaultColor;
                revAnim.AutoReverse = false;
                Storyboard.SetTarget(revAnim, panel);
                Storyboard.SetTargetProperty(revAnim, new PropertyPath(propertyPathBackground));
                revStoryboard.Children.Add(revAnim);
                revStoryboard.Begin();

                // execute release action
                if (releaseAction != null)
                    revStoryboard.Completed += new EventHandler((s, a) => { releaseAction(); });

                panel.MouseLeave -= mouseLeave;
            });
            panel.MouseLeave += mouseLeave;

            Storyboard sb = new Storyboard();
            // Background
            ColorAnimation anim = new ColorAnimation();
            anim.Duration = TimeSpan.FromSeconds(0.2);
            anim.EasingFunction = new CubicEase() { EasingMode = EasingMode.EaseInOut, };
            anim.To = highlightColor;
            anim.AutoReverse = false;
            Storyboard.SetTarget(anim, panel);
            Storyboard.SetTargetProperty(anim, new PropertyPath(propertyPathBackground));
            sb.Children.Add(anim);
            if (inAction != null)
                sb.Completed += new EventHandler((s, a) => { inAction(); });

            sb.Begin();
        }

        public static void FrameworkElementHold(FrameworkElement frameworkElement, Action releaseAction = null, double upScale = HOLD_SCALE)
        {
            if (frameworkElement == null) return;

            makeTransformGroup(frameworkElement);
            System.Windows.Input.MouseEventHandler mouseLeave = null;
            mouseLeave = new System.Windows.Input.MouseEventHandler((sender, args) =>
            {
                FrameworkElement fe = sender as FrameworkElement;
                Storyboard revStoryboard = new Storyboard();
                // Scale - X
                DoubleAnimation revAnim = new DoubleAnimation();
                revAnim.Duration = TimeSpan.FromSeconds(0.5);
                revAnim.EasingFunction = new CubicEase() { EasingMode = EasingMode.EaseOut, };
                revAnim.To = 1;
                revAnim.AutoReverse = false;
                Storyboard.SetTarget(revAnim, frameworkElement);
                Storyboard.SetTargetProperty(revAnim, new PropertyPath(propertyPathScaleX));
                revStoryboard.Children.Add(revAnim);
                // Scale - Y
                revAnim = new DoubleAnimation();
                revAnim.Duration = TimeSpan.FromSeconds(0.5);
                revAnim.EasingFunction = new CubicEase() { EasingMode = EasingMode.EaseOut, };
                revAnim.To = 1;
                revAnim.AutoReverse = false;
                Storyboard.SetTarget(revAnim, fe);
                Storyboard.SetTargetProperty(revAnim, new PropertyPath(propertyPathScaleY));
                revStoryboard.Children.Add(revAnim);
                revStoryboard.Begin();

                // execute release action
                if (releaseAction != null)
                    revStoryboard.Completed += new EventHandler((s, a) => { releaseAction(); });

                frameworkElement.MouseLeave -= mouseLeave;
            });
            frameworkElement.MouseLeave += mouseLeave;

            Storyboard sb = new Storyboard();
            // Scale - X
            DoubleAnimation anim = new DoubleAnimation();
            anim.Duration = TimeSpan.FromSeconds(0.3);
            anim.EasingFunction = new CubicEase() { EasingMode = EasingMode.EaseOut, };
            //anim.EasingFunction = new BounceEase() { EasingMode = EasingMode.EaseOut, Bounces = 1, Bounciness = 3 };
            anim.From = 1;
            anim.To = upScale;
            anim.AutoReverse = false;
            Storyboard.SetTarget(anim, frameworkElement);
            Storyboard.SetTargetProperty(anim, new PropertyPath(propertyPathScaleX));
            sb.Children.Add(anim);

            // Scale - Y
            anim = new DoubleAnimation();
            anim.Duration = TimeSpan.FromSeconds(0.3);
            anim.EasingFunction = new CubicEase() { EasingMode = EasingMode.EaseOut, };
            //anim.EasingFunction = new BounceEase() { EasingMode = EasingMode.EaseOut, Bounces = 1, Bounciness = 3 }; 
            anim.From = 1;
            anim.To = upScale;
            anim.AutoReverse = false;
            Storyboard.SetTarget(anim, frameworkElement);
            Storyboard.SetTargetProperty(anim, new PropertyPath(propertyPathScaleY));
            sb.Children.Add(anim);

            sb.Begin();
        }

        /// <summary>
        /// Ticker Item flyin animation
        /// </summary>
        /// <param name="frameworkElement">Element to animate</param>
        /// <param name="bottomToTop">true for BottomToTop, false for vice versa</param>
        public static Storyboard TickerItemFlyin(FrameworkElement frameworkElement, bool bottomToTop = true)
        {
            if (makeTransformGroup(frameworkElement, null, new TranslateTransform() { Y = bottomToTop ? TICKER_FLYIN_OFFSET : -TICKER_FLYIN_OFFSET }))
            {
                if (frameworkElement.Opacity != 0)
                    frameworkElement.Opacity = 0;
            }

            TimeSpan duration = TimeSpan.FromSeconds(TICKER_FLYIN_DURATION);
            Storyboard sb = new Storyboard();
            sb.Duration =  duration;
            DoubleAnimation slideIn = new DoubleAnimation();
            DoubleAnimation sizeX = new DoubleAnimation();
            DoubleAnimation sizeY = new DoubleAnimation();
            DoubleAnimation fadeIn = new DoubleAnimation();

            CubicEase easing = new CubicEase();
            easing.EasingMode = EasingMode.EaseOut;

            slideIn.From = bottomToTop ? TICKER_FLYIN_OFFSET : -TICKER_FLYIN_OFFSET;
            slideIn.To = 0;
            slideIn.Duration = duration;
            slideIn.AutoReverse = false;
            slideIn.EasingFunction = easing;

            sizeX.From = TICKER_SCALE_BEGIN;
            sizeX.To = 1;
            sizeX.Duration = duration;
            sizeX.AutoReverse = false;
            sizeX.EasingFunction = easing;

            sizeY.From = TICKER_SCALE_BEGIN;
            sizeY.To = 1;
            sizeY.Duration = duration;
            sizeY.AutoReverse = false;
            sizeY.EasingFunction = easing;

            fadeIn.From = 0;
            fadeIn.To = 1;
            fadeIn.Duration = duration;
            fadeIn.AutoReverse = false;
            fadeIn.EasingFunction = easing;

            Storyboard.SetTarget(slideIn, frameworkElement);
            Storyboard.SetTarget(fadeIn, frameworkElement);
            Storyboard.SetTarget(sizeX, frameworkElement);
            Storyboard.SetTarget(sizeY, frameworkElement);
            Storyboard.SetTargetProperty(slideIn, new PropertyPath(propertyPathTranslateY));
            Storyboard.SetTargetProperty(sizeX, new PropertyPath(propertyPathScaleX));
            Storyboard.SetTargetProperty(sizeY, new PropertyPath(propertyPathScaleY));
            Storyboard.SetTargetProperty(fadeIn, new PropertyPath("Opacity"));
            sb.Children.Add(slideIn);
            sb.Children.Add(fadeIn);
            sb.Children.Add(sizeX);
            sb.Children.Add(sizeY);
            sb.Begin();

            return sb;
        }

        public static void OpactiyFadeIn(FrameworkElement frameworkElement, double seconds, double opacity = -1)
        {
            TimeSpan duration = TimeSpan.FromSeconds(seconds);
            Storyboard sb = new Storyboard();
            sb.Duration = duration;
            DoubleAnimation fade = new DoubleAnimation();

            CubicEase easing = new CubicEase();
            easing.EasingMode = EasingMode.EaseOut;

            fade.From = opacity == -1 ? frameworkElement.Opacity : opacity;
            fade.To = 1;
            fade.Duration = duration;
            fade.AutoReverse = false;
            fade.EasingFunction = easing;

            Storyboard.SetTarget(fade, frameworkElement);
            Storyboard.SetTargetProperty(fade, new PropertyPath("Opacity"));
            sb.Children.Add(fade);
            sb.Begin();
        }
        public static void OpacityFadeOut(FrameworkElement frameworkElement, double seconds, double opacity = -1)
        {
            TimeSpan duration = TimeSpan.FromSeconds(seconds);
            Storyboard sb = new Storyboard();
            sb.Duration = duration;
            DoubleAnimation fade = new DoubleAnimation();

            CubicEase easing = new CubicEase();
            easing.EasingMode = EasingMode.EaseOut;
            
            fade.From = opacity == -1 ? frameworkElement.Opacity : opacity;
            fade.To = 0;
            fade.Duration = duration;
            fade.AutoReverse = false;
            fade.EasingFunction = easing;

            Storyboard.SetTarget(fade, frameworkElement);
            Storyboard.SetTargetProperty(fade, new PropertyPath("Opacity"));
            sb.Children.Add(fade);
            sb.Begin();
        }

        public static void ElementYFadeIn(FrameworkElement frameworkElement, double opacity = 1, double seconds = 0.2)
        {
            TimeSpan duration = TimeSpan.FromSeconds(seconds);
            Storyboard sb = new Storyboard();
            sb.Duration = duration;
            CubicEase easing = new CubicEase();
            easing.EasingMode = EasingMode.EaseInOut;

            makeTransformGroup(frameworkElement);

            DoubleAnimation fade = new DoubleAnimation();
            fade.To = opacity;
            fade.Duration = duration;
            fade.AutoReverse = false;
            fade.EasingFunction = easing;
            Storyboard.SetTarget(fade, frameworkElement);
            Storyboard.SetTargetProperty(fade, new PropertyPath("Opacity"));
            sb.Children.Add(fade);

            DoubleAnimation anim = new DoubleAnimation();
            anim.Duration = duration;
            anim.EasingFunction = easing;
            anim.To = 1;
            anim.AutoReverse = false;
            Storyboard.SetTarget(anim, frameworkElement);
            Storyboard.SetTargetProperty(anim, new PropertyPath(propertyPathScaleY));
            sb.Children.Add(anim);

            sb.Begin();
        }
        public static void ElementYFadeOut(FrameworkElement frameworkElement, double opacity = 0, double seconds = 0.2)
        {
            TimeSpan duration = TimeSpan.FromSeconds(seconds);
            Storyboard sb = new Storyboard();
            sb.Duration = duration;
            CubicEase easing = new CubicEase();
            easing.EasingMode = EasingMode.EaseInOut;

            makeTransformGroup(frameworkElement);

            DoubleAnimation fade = new DoubleAnimation();
            fade.To = opacity;
            fade.Duration = duration;
            fade.AutoReverse = false;
            fade.EasingFunction = easing;
            Storyboard.SetTarget(fade, frameworkElement);
            Storyboard.SetTargetProperty(fade, new PropertyPath("Opacity"));
            sb.Children.Add(fade);
            
            DoubleAnimation anim = new DoubleAnimation();
            anim.Duration = duration;
            anim.EasingFunction = easing;
            anim.To = 0;
            anim.AutoReverse = false;
            Storyboard.SetTarget(anim, frameworkElement);
            Storyboard.SetTargetProperty(anim, new PropertyPath(propertyPathScaleY));
            sb.Children.Add(anim);

            sb.Begin();
        }

        public static T getTransform<T>(object obj) where T : class
        {
            T trans = null;

            UIElement uiElement = obj as UIElement;
            if (uiElement == null) 
                throw new InvalidOperationException("obj have to be derived from UIElement");
            TransformGroup group = uiElement.RenderTransform as TransformGroup;
            if (group == null)
                throw new InvalidOperationException("obj does not contain a TransformGroup.");


            foreach (var item in group.Children)
            {
                if (item is T)
                {
                    trans = item as T;
                    break;
                }
            }

            return trans;
        }

    }
}
