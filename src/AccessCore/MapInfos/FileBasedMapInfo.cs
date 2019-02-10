namespace AccessCore.Repository.MapInfos
{
    public class FileBasedMapInfo : MapInfo
    {
        /// <summary>
        /// Mapping info file path
        /// </summary>
        protected readonly string _path;

        /// <summary>
        /// Constructs new instance of <see cref="FileBasedMapInfo"/>
        /// </summary>
        /// <param name="path">Path of mapping file.</param>
        internal FileBasedMapInfo(string path)
        {
            this._path = path;
        }
    }
}