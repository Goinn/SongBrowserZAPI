using MelonLoader;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Reflection;
using System.Text;
using System.Web;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;



namespace SongBrowser
{
    public class SongBrowser : MelonMod
    {

        public class BeatmapResponse
        {
            public BeatmapInfo[] data { get; set; }
            public int count { get; set; }
            public int total { get; set; }
            public int page { get; set; }
            public int pageCount { get; set; }
        }

        public class BeatmapInfo
        {
            public int id { get; set; }
            public string hash { get; set; }
            public string title { get; set; }
            public string artist { get; set; }
            public string mapper { get; set; }
            public string duration { get; set; }
            public string bpm { get; set; }
            public string[] difficulties { get; set; }
            public object description { get; set; }
            public string youtube_url { get; set; }
            public string filename { get; set; }
            public string filename_original { get; set; }
            public int cover_version { get; set; }
            public int play_count { get; set; }
            public int play_count_daily { get; set; }
            public int download_count { get; set; }
            public int upvote_count { get; set; }
            public int downvote_count { get; set; }
            public int vote_diff { get; set; }
            public string score { get; set; }
            public string rating { get; set; }
            public bool published { get; set; }
            public bool production_mode { get; set; }
            public bool beat_saber_convert { get; set; }
            public bool _explicit { get; set; }
            public bool ost { get; set; }
            public DateTime published_at { get; set; }
            public DateTime created_at { get; set; }
            public DateTime updated_at { get; set; }
            public int version { get; set; }
            public User user { get; set; }
            public Collaborator[] collaborators { get; set; }
            public string download_url { get; set; }
            public string cover_url { get; set; }
            public string preview_url { get; set; }
            public string video_url { get; set; }
        }

        public class User
        {
            public int id { get; set; }
            public string username { get; set; }
            public string avatar_filename { get; set; }
            public string avatar_url { get; set; }
        }

        public class Collaborator
        {
            public int id { get; set; }
            public int order { get; set; }
            public User user { get; set; }
        }

        // get beatmaps via filter and download first result
        public IEnumerator GetBeatmaps(string title="", string artist="", string mapper="", string difficulty="Master", string sortByField="published_at", string sortDir="DESC", int limit=50,  int page=1)
        {
            //download song
            string download_url = $"synthriderz.com/api/beatmaps?s={{\"title\": {{\"$cont\": \"{title}\"}}," +
                $" \"artist\": {{\"$cont\": \"{artist}\"}}, \"mapper\": {{\"$cont\": \"{mapper}\"}},\"difficulties\": {{\"$cont\": \"{difficulty}\"}}}}" +
                $"&sort={sortByField},{sortDir}&limit={limit}&page={page}";
            MelonLogger.Msg(download_url);
            using (UnityWebRequest beatmapPoll = UnityWebRequest.Get(download_url))
            {
                //string customsPath = Application.dataPath + "/../CustomSongs/";
                //beatmapPoll.downloadHandler = new DownloadHandlerFile(customsPath + "dump.synth");
                //yield return beatmapPoll.SendWebRequest();
                beatmapPoll.SendWebRequest();
                if (beatmapPoll.isNetworkError | beatmapPoll.isHttpError)
                {
                    MelonLogger.Msg("GetSong error");
                    yield return null;
                }
                else
                {
                    MelonLogger.Msg("Download successful");
                    //rename file
                    BeatmapResponse beatmapInfo = JsonConvert.DeserializeObject<BeatmapResponse>(beatmapPoll.downloadHandler.text);

                    //ssmInstance.RefreshSongList(false);
                    MelonLogger.Msg(beatmapInfo.data[0].title + " - " + beatmapInfo.data[0].artist);
                    yield return beatmapInfo;
                }
            }
        }

        // download song via hash
        public IEnumerator GetBeatmap(string hash)
        {
            string download_url = "synthriderz.com/api/beatmaps/hash/download/" + hash;
            MelonLogger.Msg(download_url);
            using (UnityWebRequest songRequest = UnityWebRequest.Get(download_url))
            {
                string customsPath = Application.dataPath + "/../CustomSongs/";
                songRequest.downloadHandler = new DownloadHandlerFile(customsPath + "dump.synth");
                yield return songRequest.SendWebRequest();
                if (songRequest.isNetworkError | songRequest.isHttpError)
                {
                    MelonLogger.Msg("GetSong error");
                }
                else
                {
                    MelonLogger.Msg("Download successful");
                    //rename file
                    if (File.Exists(customsPath + "dump.synth"))
                    {
                        string fileName = songRequest.GetResponseHeader("content-disposition").Split('"')[1];
                        MelonLogger.Msg(fileName);
                        File.Move(customsPath + "dump.synth", customsPath + fileName);
                    }
                    //ssmInstance.RefreshSongList(false);
                    //MelonLogger.Msg("Updated song list");
                }
            }
        }

        // Download song via title, artist and beatmapper
        public IEnumerator GetSong(string title, string artist, string beatmapper)
        {
            Type ssm = typeof(Synth.SongSelection.SongSelectionManager);
            FieldInfo ssmInstanceInfo = ssm.GetField("s_instance", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
            Synth.SongSelection.SongSelectionManager ssmInstance = (Synth.SongSelection.SongSelectionManager)ssmInstanceInfo.GetValue(null);
            artist.Replace("&", "%26");
            string requestUrl = $"synthriderz.com/api/beatmaps?s={{\"title\": \"{title}\", \"artist\": \"{artist}\", \"mapper\": \"{beatmapper}\"}}";
            using (UnityWebRequest webRequest = UnityWebRequest.Get(requestUrl))
            {
                yield return webRequest.SendWebRequest();
                //convert to json?
                //get hash
                //download map -> synthriderz.com/api/veatnaos/hash/download/ + hash
                if (webRequest.isNetworkError)
                {
                    MelonLogger.Msg("GetSong error");
                }
                else
                {
                    BeatmapInfo[] beatmapInfo = JsonConvert.DeserializeObject<BeatmapInfo[]>(webRequest.downloadHandler.text);


                    if (beatmapInfo.Length == 1)
                    {
                        //download song
                        MelonLogger.Msg("Found beatmap");
                        string download_url = "synthriderz.com" + beatmapInfo[0].download_url;
                        MelonLogger.Msg(download_url);
                        using (UnityWebRequest songRequest = UnityWebRequest.Get(download_url))
                        {
                            string customsPath = Application.dataPath + "/../CustomSongs/" + beatmapInfo[0].filename;
                            songRequest.downloadHandler = new DownloadHandlerFile(customsPath);
                            yield return songRequest.SendWebRequest();
                            if (webRequest.isNetworkError)
                            {
                                MelonLogger.Msg("GetSong error");
                            }
                            else
                            {
                                MelonLogger.Msg("Download successful");
                                ssmInstance.RefreshSongList(false);
                                MelonLogger.Msg("Updated song list");
                            }
                        }
                    }
                    else
                    {
                        MelonLogger.Msg("Found more than one or no beatmaps");
                    }
                }
            }
        }

        public override void OnUpdate()
        {
            // Testing
            if (Input.GetKeyDown(KeyCode.T))
            {
                LoggerInstance.Msg("You just pressed T");
                //create callback 
                BeatmapResponse re = (BeatmapResponse)MelonCoroutines.Start(GetBeatmaps());
            }
        }

    }
}
