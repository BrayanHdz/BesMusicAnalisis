using MusicPlayer.Core;
using MusicPlayer.Viewmodels;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Foundation.Metadata;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;

// Die Elementvorlage "Benutzersteuerelement" wird unter https://go.microsoft.com/fwlink/?LinkId=234236 dokumentiert.

namespace MusicPlayer.Controls
{
    public sealed partial class Albums : UserControl
    {

        public AlbumCollectionViewmodel AlbumViewmodel => AlbumCollectionViewmodel.Instance;



        public bool IsAlbumDisplayedLarge
        {
            get { return (bool)this.GetValue(IsAlbumDisplayedLargeProperty); }
            set { this.SetValue(IsAlbumDisplayedLargeProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IsAlbumDisplayedLarge.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsAlbumDisplayedLargeProperty =
            DependencyProperty.Register("IsAlbumDisplayedLarge", typeof(bool), typeof(Albums), new PropertyMetadata(false));





        public double AlbumWidth
        {
            get { return (double)this.GetValue(AlbumWidthProperty); }
            set { this.SetValue(AlbumWidthProperty, value); }
        }

        // Using a DependencyProperty as the backing store for AlbumWidth.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty AlbumWidthProperty =
            DependencyProperty.Register("AlbumWidth", typeof(double), typeof(Albums), new PropertyMetadata(166));




        public Albums()
        {
            this.InitializeComponent();

            this.Loaded += this.Albums_Loaded;
        }

        private void Albums_Loaded(object sender, RoutedEventArgs e)
        {
            //await LocalLibrary.Instance.Update(default);
            this.UpdateSize(new Size(this.ActualWidth, this.ActualHeight));

        }

        private async void ToRender_ContainerContentChanging(ListViewBase sender, ContainerContentChangingEventArgs args)
        {
            var root = args.ItemContainer.ContentTemplateRoot as FrameworkElement;
            var image = root.FindName("cover") as ImageBrush;
            var vm = args.Item as AlbumViewmodel;
            if (args.Phase == 0)
            {
                var oldCancel = root.Tag as CancellationTokenSource;
                if (oldCancel != null)
                {
                    oldCancel.Cancel();
                    oldCancel.Dispose();
                }
                var cancel = new CancellationTokenSource();
                args.RegisterUpdateCallback(this.ToRender_ContainerContentChanging);
                root.Tag = cancel;
                image.Opacity = 0;
            }
            else if (args.Phase == 1)
            {
                var cancel = root.Tag as CancellationTokenSource;
                var imageSource = await vm.LoadCoverAsync(cancel.Token);
                if (!cancel.IsCancellationRequested)
                {
                    image.ImageSource = imageSource;
                    image.Opacity = 1;
                }

            }

            args.Handled = true;

        }

        private void AlbumClicked(object sender, ItemClickEventArgs e)
        {
            var item = e.ClickedItem as AlbumViewmodel;
            var container = this.toRender.ContainerFromItem(e.ClickedItem) as GridViewItem;


            var root = container.ContentTemplateRoot as FrameworkElement;
            var cover = root.FindName("coverBorder") as UIElement;
            var name = root.FindName("name") as UIElement;

            ConnectedAnimationService.GetForCurrentView()
                .PrepareToAnimate("forwardAnimationCover", cover);
            ConnectedAnimationService.GetForCurrentView()
                            .PrepareToAnimate("forwardAnimationName", name);

            Services.NavigationService.Navigate<Pages.AlbumPage>(item);
        }
        private childItem FindVisualChild<childItem>(DependencyObject obj)
            where childItem : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(obj, i);
                if (child != null && child is childItem)
                    return (childItem)child;
                else
                {
                    childItem childOfChild = this.FindVisualChild<childItem>(child);
                    if (childOfChild != null)
                        return childOfChild;
                }
            }
            return null;
        }

        private void UserControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            var newSize = e.NewSize;
            this.UpdateSize(newSize);
        }

        private void UpdateSize(Size newSize)
        {
            this.IsAlbumDisplayedLarge = newSize.Width < 364 && newSize.Width > 260;
            if (!this.IsAlbumDisplayedLarge)
                this.ClearValue(AlbumWidthProperty);
            else
            {
                this.AlbumWidth = newSize.Width - 16;
            }
        }

        private async void ExceptionHandlerConverter_OnError(Exception exception)
        {
            var dialog = new MessageDialog(exception.Message);
            await dialog.ShowAsync();
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            var songs = MusicStore.Instance.Albums
              .SelectMany(x => x.Songs)
              .Where(x => x.Availability != Availability.NotAvailable)
              .Select(x => x.Songs.FirstOrDefault(y => y.Availability != Availability.NotAvailable));

            await MediaplayerViewmodel.Instance.ResetSongs(songs.ToImmutableArray());
        }

        private void ItemsWrapGrid_BringIntoViewRequested(UIElement sender, BringIntoViewRequestedEventArgs args)
        {
            if (args.TargetRect.Height <= 246)
            {

                var t = args.TargetRect;
                t = new Rect(t.X, t.Y, t.Width, t.Height + Helpers.ConstantsHelper.PlayListHeightField);
                args.TargetRect = t;
            }
        }

        private void ItemsPanel_Loaded(object sender, RoutedEventArgs e)
        {
            if (ApiInformation.IsEventPresent("Windows.UI.Xaml.UIElement", nameof(this.PreviewKeyDown)))
            {
                var panel = sender as UIElement;
                panel.BringIntoViewRequested += this.ItemsWrapGrid_BringIntoViewRequested;
            }
        }
    }
}
