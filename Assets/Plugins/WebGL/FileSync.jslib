// Browsers keep files written under the persistent data path in memory until they are flushed to IndexedDB.
// SaveService calls this after every write so progress survives closing the tab.
mergeInto(LibraryManager.library, {
  FileSync_Flush: function () {
    if (typeof FS !== 'undefined' && FS.syncfs) {
      FS.syncfs(false, function (error) {
        if (error) {
          console.error('FileSync_Flush failed', error);
        }
      });
    }
  }
});
