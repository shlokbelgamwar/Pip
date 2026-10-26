// Light system haptics for Pip's UI. Called from PipHaptics.cs on iOS devices only.
#import <UIKit/UIKit.h>

extern "C" {
void PipHapticImpact(int style) {
    dispatch_async(dispatch_get_main_queue(), ^{
        UIImpactFeedbackGenerator *generator = [[UIImpactFeedbackGenerator alloc] initWithStyle:(UIImpactFeedbackStyle)style];
        [generator impactOccurred];
    });
}

void PipHapticNotify(int type) {
    dispatch_async(dispatch_get_main_queue(), ^{
        UINotificationFeedbackGenerator *generator = [[UINotificationFeedbackGenerator alloc] init];
        [generator notificationOccurred:(UINotificationFeedbackType)type];
    });
}

void PipHapticSelection(void) {
    dispatch_async(dispatch_get_main_queue(), ^{
        UISelectionFeedbackGenerator *generator = [[UISelectionFeedbackGenerator alloc] init];
        [generator selectionChanged];
    });
}
}
