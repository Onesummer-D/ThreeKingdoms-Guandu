// Unity WebGL browser download bridge. This runs only in a WebGL build.
// The C# call is triggered by a real player click on "下载海报".
mergeInto(LibraryManager.library, {
  GuanduDownloadPoster: function (dataPointer, dataLength, fileNamePointer) {
    if (!dataPointer || dataLength <= 0) return;
    var safeName = UTF8ToString(fileNamePointer) || 'GuanduReport.png';
    var bytes = HEAPU8.slice(dataPointer, dataPointer + dataLength);
    var blob = new Blob([bytes], { type: 'image/png' });
    var url = URL.createObjectURL(blob);
    var anchor = document.createElement('a');
    anchor.href = url;
    anchor.download = safeName;
    anchor.style.display = 'none';
    document.body.appendChild(anchor);
    try {
      anchor.click();
    } finally {
      document.body.removeChild(anchor);
      // Leave the object URL alive while the browser begins the save.
      setTimeout(function () { URL.revokeObjectURL(url); }, 30000);
    }
  }
});
