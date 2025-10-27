using System.Threading;
using System.Threading.Tasks;
using FishingGame.Domain.Interfaces;
using FishingGame.Domain.Struct;
using FishingGame.Domain.Enums;

namespace FishingGame.Controller
{
    /// <summary>Implémentation de IHumanInput utilisant un TaskCompletionSource.</summary>
    public sealed class HumanInput : IHumanInput
    {
        private TaskCompletionSource<(Card? card, COLOR_TYPE? chosenColor)>? _tcs;
        public Task<(Card? card, COLOR_TYPE? chosenColor)> WaitForActionAsync(CancellationToken token)
        {
            _tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
            if (token.CanBeCanceled)
                token.Register(() => _tcs?.TrySetCanceled(token));
            return _tcs.Task;
        }
        public void SubmitPlay(Card card, COLOR_TYPE? chosenColor) => _tcs?.TrySetResult((card, chosenColor));
        public void SubmitDraw() => _tcs?.TrySetResult((null, null));
        public void Reset() { _tcs?.TrySetCanceled(); _tcs = null; }
    }
}