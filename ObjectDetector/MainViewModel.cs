using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Humanizer;
using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Prediction;
using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Prediction.Models;
using Plugin.Media;
using Plugin.Media.Abstractions;
using Plugin.TextToSpeech;
using SkiaSharp;
using Xamarin.Forms;

namespace ObjectDetector
{
    public class MainViewModel : BaseViewModel
    {
        PredictionEndpoint endpoint;

        public MainViewModel()
        {
            TakePhotoCommand = new Command(async () => await TakePhoto());
            PickPhotoCommand = new Command(async () => await PickPhoto());

            endpoint = new PredictionEndpoint
            {
                ApiKey = ApiKeys.PredictionKey
            };
        }

        SKBitmap image;
        public SKBitmap Image
        {
            get => image;
            set => Set(ref image, value);
        }

        bool isEnabled = true;
        public bool IsEnabled
        {
            get => isEnabled;
            set
            {
                if (Set(ref isEnabled, value))
                    OnPropertyChanged(nameof(IsBusy));

            }
        }
        public bool IsBusy => !IsEnabled;

        double probability = .75;
        public double Probability
        {
            get => probability;
            set
            {
                if (Set(ref probability, value))
                    OnPropertyChanged(nameof(ProbabilityText));
            }
        }

        public string ProbabilityText => $"{Probability:P0}";

        List<PredictionModel> predictions;
        public List<PredictionModel> Predictions
        {
            get => predictions;
            set => Set(ref predictions, value);
        }

        public ICommand TakePhotoCommand { get; }
        public ICommand PickPhotoCommand { get; }

        Task TakePhoto() => GetPhoto(() => CrossMedia.Current.TakePhotoAsync(new StoreCameraMediaOptions { PhotoSize = PhotoSize.Small, SaveMetaData = false }));
        Task PickPhoto() => GetPhoto(() => CrossMedia.Current.PickPhotoAsync(new PickMediaOptions { PhotoSize = PhotoSize.Small, SaveMetaData = false }));

        async Task GetPhoto(Func<Task<MediaFile>> getPhotoFunc)
        {
            IsEnabled = false;

            Image = null;
            Predictions = null;

            try
            {
                var photo = await getPhotoFunc();
                if (photo == null) return;


                Image = SKBitmap.Decode(photo.GetStream());
                await PredictPhoto(photo);

                IsEnabled = true;
                await SayWhatYouSee();
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", $"An error occured: {ex.Message}", "OK");
            }
            finally
            {
                IsEnabled = true;
            }
        }

        async Task PredictPhoto(MediaFile photo)
        {
            var results = await endpoint.PredictImageAsync(Guid.Parse(ApiKeys.ProjectId), photo.GetStream());
            Predictions = results.Predictions
                                 .Where(p => p.Probability > Probability)
                                 .ToList();
        }

        async Task SayWhatYouSee()
        {
            var text = "";

            try
            {
                if (Predictions.Any())
                {
                    if (Predictions.Count == 1)
                        text = $"I see {Predictions[0].TagName.Humanize()}";
                    else
                    {
                        text = "I see ";
                        for (var i = 0; i < Predictions.Count - 1; ++i)
                            text += Predictions[i].TagName.Humanize() + ", ";
                        text += $"and {Predictions.Last().TagName.Humanize()}";

                    }
                }
                else
                {
                    text = "I don't see anything I recognise";
                }

                await CrossTextToSpeech.Current.Speak(text);
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", $"An error occured: {ex.Message}", "OK");
            }
        }
    }
}
