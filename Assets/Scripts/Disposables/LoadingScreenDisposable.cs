using System;
using UnityEngine;
using UnityEngine.UI;

namespace Disposables
{
    /// <summary>
    /// Manages the lifecycle of a loading screen display and allows updating the loading progress.
    /// </summary>
    public class LoadingScreenDisposable : IDisposable
    {
        // Private field for the loading screen GameObject
        private readonly GameObject loadingScreen;
        private readonly Slider loadingBar; 

        /// <summary>
        /// Initializes a new instance of <see cref="LoadingScreenDisposable"/>, displays the loading screen, and prepares for loading bar updates.
        /// </summary>
        /// <param name="loadingScreen">The loading screen GameObject to manage.</param>
        public LoadingScreenDisposable(GameObject loadingScreen)
        {
            // Assign the loading screen GameObject to the private field
            this.loadingScreen = loadingScreen;
        
            // Show the loading screen by enabling the GameObject
            this.loadingScreen.SetActive(true);

            // Attempt to locate a Slider component for the loading bar within the loading screen
            loadingBar = loadingScreen.GetComponentInChildren<Slider>();
            if (loadingBar == null)
            {
                throw new InvalidOperationException("No loading bar (Slider) found in the loading screen GameObject.");
            }
        }

        /// <summary>
        /// Sets the loading progress on the loading bar.
        /// </summary>
        /// <param name="percent">The percentage of loading progress, expected to be between 0 and 1.</param>
        public void SetLoadingBarPercent(float percent)
        {
            if (loadingBar != null)
            {
                loadingBar.value = Mathf.Clamp01(percent); // Ensures the value is between 0 and 1
            }
        }

        /// <summary>
        /// Disposes of the <see cref="LoadingScreenDisposable"/> instance, hiding the loading screen.
        /// </summary>
        public void Dispose()
        {
            // Hide the loading screen by disabling the GameObject
            if (loadingScreen != null)
            {
                loadingScreen.SetActive(false);
            }
        }
    }
}
