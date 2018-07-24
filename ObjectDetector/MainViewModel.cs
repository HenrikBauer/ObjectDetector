using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Prediction;
using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Prediction.Models;
using Plugin.Media;
using Plugin.Media.Abstractions;
using SkiaSharp;
using Xamarin.Forms;

namespace ObjectDetector
{
	public class MainViewModel : INotifyPropertyChanged
    {
        bool Set<T>(ref T field, T value, [CallerMemberName]string propertyName = null)
        {
            if (Equals(field, value)) return false;
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            return true;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        PredictionEndpoint endpoint;

        public MainViewModel()
        {
            TakePhotoCommand = new Command(async () => await TakePhoto());
            endpoint = new PredictionEndpoint
            {
                ApiKey = ApiKeys.PredictionKey
            };
        }

        SKBitmap bitmap;
        public SKBitmap Image
        {
            get => bitmap;
            set => Set(ref bitmap, value);
        }

        bool isEnabled = true;
        public bool IsEnabled
        {
            get => isEnabled;
            set => Set(ref isEnabled, value);
        }

        List<PredictionModel> predictions = new List<PredictionModel>();
        public List<PredictionModel> Predictions
        {
            get => predictions;
            set => Set(ref predictions, value);
        }

        public ICommand TakePhotoCommand { get; }

        async Task TakePhoto()
        {
            IsEnabled = false;

            try
            {
                var options = new StoreCameraMediaOptions { PhotoSize = PhotoSize.Medium };
                var photo = await CrossMedia.Current.TakePhotoAsync(options);

                var results = await endpoint.PredictImageAsync(Guid.Parse(ApiKeys.ProjectId),
                                                               photo.GetStream());

                Predictions = results.Predictions
                                     .Where(p => p.Probability > 0.75)
                                     .ToList();

                Image = SKBitmap.Decode(photo.GetStream());
            }
            finally
            {
                IsEnabled = true;
            }
        }
    }
}
