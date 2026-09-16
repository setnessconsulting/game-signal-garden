mergeInto(LibraryManager.library, {
  SignalGardenInstallPointerCapture: function () {
    var canvas = (typeof Module !== "undefined" && Module.canvas) || document.querySelector("canvas");
    if (!canvas || canvas.__signalGardenCaptureInstalled) {
      return;
    }

    canvas.__signalGardenCaptureInstalled = true;
    canvas.addEventListener("pointerdown", function (event) {
      if (event.button === 0 && canvas.setPointerCapture) {
        try {
          canvas.setPointerCapture(event.pointerId);
        } catch (error) {
          // The browser may already have released this pointer.
        }
      }
    });
    canvas.addEventListener("pointerup", function (event) {
      if (canvas.hasPointerCapture && canvas.hasPointerCapture(event.pointerId)) {
        canvas.releasePointerCapture(event.pointerId);
      }
    });
    canvas.addEventListener("pointercancel", function (event) {
      if (canvas.hasPointerCapture && canvas.hasPointerCapture(event.pointerId)) {
        canvas.releasePointerCapture(event.pointerId);
      }
    });
  },

  SignalGardenSetAccessibleStatus: function (messagePointer) {
    var status = document.getElementById("signal-garden-accessible-status");
    if (!status) {
      status = document.createElement("div");
      status.id = "signal-garden-accessible-status";
      status.setAttribute("role", "status");
      status.setAttribute("aria-live", "polite");
      status.setAttribute("aria-atomic", "true");
      status.tabIndex = 0;
      status.style.position = "absolute";
      status.style.width = "1px";
      status.style.height = "1px";
      status.style.padding = "0";
      status.style.margin = "-1px";
      status.style.overflow = "hidden";
      status.style.clipPath = "inset(50%)";
      status.style.whiteSpace = "nowrap";
      status.style.border = "0";
      var stage = document.querySelector("[data-game-stage]") || document.body;
      stage.appendChild(status);

      var style = document.createElement("style");
      style.textContent =
        "#signal-garden-accessible-status:focus{position:absolute!important;left:12px!important;bottom:12px!important;width:auto!important;height:auto!important;padding:10px 14px!important;margin:0!important;overflow:visible!important;clip-path:none!important;white-space:normal!important;z-index:10000!important;color:#f3f4e7!important;background:#102323!important;border:1px solid #8bbbaa!important;border-radius:8px!important;font:16px/1.4 system-ui,sans-serif!important}";
      document.head.appendChild(style);
    }

    status.textContent = UTF8ToString(messagePointer);
  },

  SignalGardenReportFrameRate: function (framesPerSecond) {
    window.signalGardenFrameRate = framesPerSecond;
    document.documentElement.setAttribute("data-signal-garden-fps", framesPerSecond);
  }
});
