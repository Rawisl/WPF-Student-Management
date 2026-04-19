using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace WPF_Student_Management.Helpers
{
    public static class Permission
    {
        public static readonly DependencyProperty FeatureProperty =
            DependencyProperty.RegisterAttached(
                "Feature",
                typeof(PermissionService.Feature?),
                typeof(Permission),
                new PropertyMetadata(null, OnFeatureChanged));

        public static void SetFeature(UIElement element, PermissionService.Feature? value)
            => element.SetValue(FeatureProperty, value);

        public static PermissionService.Feature? GetFeature(UIElement element)
            => (PermissionService.Feature?)element.GetValue(FeatureProperty);

        private static void OnFeatureChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is UIElement element && e.NewValue is PermissionService.Feature feature)
            {
                // Check if the user has the required permission for this feature from PermissionService
                bool hasPermission = PermissionService.HasFeature(feature);

                // Visibility logic: show element if user has permission, otherwise hide it
                element.Visibility = hasPermission ? Visibility.Visible : Visibility.Collapsed;
            }
        }
    }
}
