using System;

namespace ChatAPI
{
    public interface IProgress
    {
        void SetProgress(bool success, int current, int max);
        void TimedOut();
    }
}
