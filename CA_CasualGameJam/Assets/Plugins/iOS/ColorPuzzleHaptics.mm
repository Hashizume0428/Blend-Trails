#import <UIKit/UIKit.h>
#import <dispatch/dispatch.h>

extern "C" void CP_LightTap()
{
    void (^feedback)(void) = ^{
        if (@available(iOS 10.0, *))
        {
            static UIImpactFeedbackGenerator *generator;
            if (generator == nil)
                generator = [[UIImpactFeedbackGenerator alloc] initWithStyle:UIImpactFeedbackStyleLight];
            [generator impactOccurred];
            [generator prepare];
        }
    };
    if ([NSThread isMainThread]) feedback();
    else dispatch_async(dispatch_get_main_queue(), feedback);
}
