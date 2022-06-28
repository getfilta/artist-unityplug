using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Filta.Datatypes {
    public class ArtMeta {
        public string artId;
        public string artist;
        public int version;
        public string creationTime;
        public string preview;
        public string publishTime;
        public string title;
        public string description;
        public string thumbFilename;
        public Sprite Thumbnail;
        public bool isPublic;
        public int bundleQueuePosition;
        public string bundleStatus;
        public long lastUpdated;

        public override string ToString() {
            return $"artid:{artId} artist:{artist} creationTime:{creationTime} preview:{preview} publishtime:{publishTime} title:{title} description:{description}";
        }
    }
}
