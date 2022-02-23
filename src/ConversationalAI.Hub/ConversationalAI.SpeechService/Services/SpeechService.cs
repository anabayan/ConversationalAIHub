using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using ConversationalAI.Infrastructure.Interfaces.Services;
using ConversationalAI.SpeechService.Models.Config;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.Extensions.Options;

namespace ConversationalAI.SpeechService.Services
{
    public class SpeechService : ISpeechService
    {
        private readonly SpeechConfig _speechConfig;
        // private readonly  SpeechServiceSettings _speechServiceSettings;
        
        public SpeechService(IOptionsMonitor<SpeechServiceSettings> speechSettings)
        {
            var speechServiceSettings = speechSettings.CurrentValue;
            _speechConfig = SpeechConfig.FromSubscription(speechServiceSettings.SubscriptionKey, speechServiceSettings.AzureRegion);
            _speechConfig.SpeechRecognitionLanguage = speechServiceSettings.Language;
        }

        public async Task<string> GetSpeech(string message)
        {
            return await ConvertTextToSpeechBase64(message);
        }

        public string ConvertToBase64(byte[] audioBytes)
        {
            try
            {
                return Convert.ToBase64String(audioBytes);
            }
            catch (Exception e)
            {
                var message = $"An error occurred converting the {nameof(audioBytes)}. {e.Message}";
                throw;
            }
        }

        public async Task<string> ConvertTextToSpeechBase64(string text)
        {
            var data = await ConvertTextToSpeechAsync(text);
            return ConvertToBase64(data);
        }

        public async Task<byte[]> ConvertTextToSpeechAsync(string text)
        {
            
            using var synthesizer = new SpeechSynthesizer(_speechConfig);
            using var result = await synthesizer.SpeakTextAsync(text);
            
            switch (result.Reason)
            {
                case ResultReason.SynthesizingAudioCompleted:
                    return result.AudioData;
                case ResultReason.Canceled:
                {
                    var cancellation = SpeechSynthesisCancellationDetails.FromResult(result);
                    Console.WriteLine($"CANCELED: Reason={cancellation.Reason}");

                    if (cancellation.Reason == CancellationReason.Error)
                    {
                        Console.WriteLine($"CANCELED: ErrorCode={cancellation.ErrorCode}");
                        Console.WriteLine($"CANCELED: ErrorDetails=[{cancellation.ErrorDetails}]");
                        Console.WriteLine($"CANCELED: Did you update the subscription info?");
                    }
                    
                    throw new Exception("Speech synthesis canceled");
                }
                default:
                    throw new Exception("Speech synthesis failed");
            }
        }

        public async Task<string> ConvertSpeechToTextAsync([NotNull] string audioBase64)
        {
            var audioBytes = Convert.FromBase64String(audioBase64);
            return await ConvertSpeechToTextAsync(audioBytes);
        }

        public async Task<string> ConvertSpeechToTextAsync([NotNull] byte[] audioBytes)
        {
            // var reader = new BinaryReader(new MemoryStream(audioBytes));
            using var audioInputStream = AudioInputStream.CreatePushStream();
            using var audioConfig = AudioConfig.FromStreamInput(audioInputStream);
            using var recognizer = new SpeechRecognizer(_speechConfig, audioConfig);
            
            //
            // do
            // {
            //     audioBytes = reader.ReadBytes(1024);
            //     audioInputStream.Write(audioBytes, audioBytes.Length);
            // } while (audioBytes.Length > 0);

            audioInputStream.Write(audioBytes, audioBytes.Length);
            var result = await recognizer.RecognizeOnceAsync();
            return result.Text;
        }
    }
}