﻿using System;
using System.Collections;
using ModIO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ModIOBrowser.Implementation
{
    /// <summary>
    /// This is used for the slots that move in the home view carousel for showing featured mods.
    /// These are never instantiated but a set amount of them are fixed on the carousel and are
    /// hydrated according to the carousel position.
    /// </summary>
    internal class FeaturedModListItem : ListItem
    {
        [SerializeField] Image image;
        [SerializeField] GameObject background;
        [SerializeField] GameObject failedToLoad;
        [SerializeField] TMP_Text featuredSelectedName;
        public SubscribedProgressTab progressTab;

        public int rowIndex;
        public int profileIndex;

        static float transitionTime = 0.5f;
        public AnimationCurve animationCurve = new AnimationCurve(
            new Keyframe(0, 0, 0, 2f), new Keyframe(1f, 1f));

        IEnumerator transition;
        internal static int transitionCount = 0;
        internal Translation featuredSelectedPriceTranslation = null;
#region Overrides
        public override void PlaceholderSetup()
        {
            base.PlaceholderSetup();
            image.color = Color.clear;
            background.SetActive(false);
            failedToLoad.SetActive(false);
        }

        public override void Setup(ModProfile modProfile)
        {
            base.Setup();
            profile = modProfile;
            featuredSelectedName.text = modProfile.name;
            progressTab.Setup(modProfile);
            image.color = Color.clear;
            background.SetActive(false);
            failedToLoad.SetActive(false);
            ModIOUnity.DownloadTexture(modProfile.logoImage640x360, SetIcon);
        }
#endregion // Overrides

        // TODO Move the following two methods somewhere more generic like a utilities class
        void SetIcon(ResultAnd<Texture2D> resultAndTexture)
        {
            if(resultAndTexture.result.Succeeded() && resultAndTexture != null)
            {
                QueueRunner.Instance.AddSpriteCreation(resultAndTexture.value, sprite =>
                {
                    image.sprite = sprite;
                    image.color = Color.white;
                    background.SetActive(true);
                });
            }
            else
            {
                failedToLoad.SetActive(true);
            }
        }

        public void Transition(RectTransform start, RectTransform end)
        {
            if(transition != null)
            {
                // stop existing coroutine
                StopCoroutine(transition);

                // begin new coroutine but use current position/size as starting point if our
                // current position takes us in the same direction (so we dont break pattern and
                // potentially transition back across the face of the featured view)
                bool isDirectionLeft = start.position.x > end.position.x;
                bool isDirectionFromCurrentPositionLeft = transform.position.x > end.position.x;

                if(isDirectionLeft != isDirectionFromCurrentPositionLeft)
                {
                    transition = Transition(start.position, end);
                }
                else
                {
                    transition = Transition(transform.position, end);
                }
                StartCoroutine(transition);
            }
            else
            {
                // cache for efficiency
                Transform t = transform;
                Vector2 position = start.position;

                // set starting position and size
                t.position = position;
                RectTransform rectTransform = (RectTransform)t;
                rectTransform.sizeDelta = start.sizeDelta;

                // begin coroutine transition
                transition = Transition(position, end);
                StartCoroutine(transition);
            }
        }

        IEnumerator Transition(Vector2 start, RectTransform end)
        {
            Home.Instance.HideFeaturedHighlight();
            transitionCount++;
            RectTransform rectTransform = (RectTransform)transform;
            Vector2 startingSize = rectTransform.sizeDelta;
            Vector2 distance = (Vector2)end.position - start;
            Vector2 growth = end.sizeDelta - startingSize;
            float timePassed = 0f;

            while(timePassed <= transitionTime)
            {
                timePassed += Time.unscaledDeltaTime;

                float delta = animationCurve.Evaluate(timePassed / transitionTime);

                Vector3 positionNow = start + distance * delta;

                //preserve the Y position, we are only swiping horizontally
                positionNow.y = transform.position.y;

                transform.position = positionNow;
                rectTransform.sizeDelta = startingSize + growth * delta;

                yield return null;
            }
            yield return new WaitForSecondsRealtime(0.01f);

            transitionCount--;
            if(transitionCount == 0)
            {
                Home.Instance.ShowFeaturedHighlight();
            }
        }

    }
}
