#import <Foundation/Foundation.h>
#import <Photos/Photos.h>
extern "C" void UnitySendMessage(const char *obj, const char *method, const char *arg);

static void PipPhotoResult(NSString *receiver, NSString *result) {
    dispatch_async(dispatch_get_main_queue(), ^{
        UnitySendMessage(receiver.UTF8String, "OnPhotoSaved", result.UTF8String);
    });
}
extern "C" void PipSaveMemoryToPhotos(const char *path, const char *receiverName) {
    NSString *file = path ? [NSString stringWithUTF8String:path] : nil;
    NSString *receiver = receiverName ? [NSString stringWithUTF8String:receiverName] : nil;
    if (!receiver) return;
    if (!file || ![[NSFileManager defaultManager] fileExistsAtPath:file]) { PipPhotoResult(receiver, @"failed"); return; }
    dispatch_async(dispatch_get_main_queue(), ^{
        [PHPhotoLibrary requestAuthorizationForAccessLevel:PHAccessLevelAddOnly handler:^(PHAuthorizationStatus status) {
            if (status != PHAuthorizationStatusAuthorized && status != PHAuthorizationStatusLimited) { PipPhotoResult(receiver, @"denied"); return; }
            [[PHPhotoLibrary sharedPhotoLibrary] performChanges:^{
                [PHAssetChangeRequest creationRequestForAssetFromImageAtFileURL:[NSURL fileURLWithPath:file]];
            } completionHandler:^(BOOL success, NSError *error) {
                PipPhotoResult(receiver, success ? @"saved" : @"failed");
            }];
        }];
    });
}
