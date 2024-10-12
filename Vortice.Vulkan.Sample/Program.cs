//using System;
//using System.Text;
//using System.Runtime.InteropServices;
//using Vortice.Vulkan;
//using static Vortice.Vulkan.Vulkan;

//namespace VulkanCoreSetup
//{
//    class Program
//    {
//        static unsafe void Main(string[] args)
//        {
//            // Application and engine names
//            string appName = "Vulkan Vortice Example";
//            string engineName = "Vortice Engine";

//            // Get the bytes of the strings, including null terminator
//            byte[] appNameBytes = Encoding.ASCII.GetBytes(appName + '\0');
//            byte[] engineNameBytes = Encoding.ASCII.GetBytes(engineName + '\0');

//            // Layer names
//            string[] layers = { "VK_LAYER_KHRONOS_validation" };
//            byte[][] layerNameBytes = new byte[layers.Length][];
//            for (int i = 0; i < layers.Length; i++)
//            {
//                layerNameBytes[i] = Encoding.ASCII.GetBytes(layers[i] + '\0');
//            }

//            try
//            {
//                fixed (byte* appNamePtr = appNameBytes)
//                fixed (byte* engineNamePtr = engineNameBytes)
//                {
//                    // Pin the layer name strings
//                    GCHandle[] layerHandles = new GCHandle[layers.Length];
//                    byte*[] ppEnabledLayerNamesArray = new byte*[layers.Length];

//                    for (int i = 0; i < layers.Length; i++)
//                    {
//                        layerHandles[i] = GCHandle.Alloc(layerNameBytes[i], GCHandleType.Pinned);
//                        ppEnabledLayerNamesArray[i] = (byte*)layerHandles[i].AddrOfPinnedObject();
//                    }

//                    // Pin the array of layer name pointers
//                    fixed (byte** ppEnabledLayerNames = ppEnabledLayerNamesArray)
//                    {
//                        // Create the VkApplicationInfo structure
//                        VkApplicationInfo appInfo = new VkApplicationInfo
//                        {
//                            sType = VkStructureType.ApplicationInfo,
//                            pNext = null,
//                            pApplicationName = appNamePtr,
//                            applicationVersion = VkMakeVersion(1, 0, 0),
//                            pEngineName = engineNamePtr,
//                            engineVersion = VkMakeVersion(1, 0, 0),
//                            apiVersion = VkMakeVersion(1, 0, 0)
//                        };

//                        // No need to fix 'appInfo' since it's an unmanaged local variable
//                        VkApplicationInfo* pAppInfo = &appInfo;

//                        // Create VkInstanceCreateInfo structure
//                        VkInstanceCreateInfo createInfo = new VkInstanceCreateInfo
//                        {
//                            sType = VkStructureType.InstanceCreateInfo,
//                            pNext = null,
//                            flags = 0,
//                            pApplicationInfo = pAppInfo,
//                            enabledLayerCount = (uint)layers.Length,
//                            ppEnabledLayerNames = ppEnabledLayerNames,
//                            enabledExtensionCount = 0,
//                            ppEnabledExtensionNames = null
//                        };

//                        // No need to fix 'createInfo' for the same reason
//                        VkInstanceCreateInfo* pCreateInfo = &createInfo;

//                        // Create Vulkan instance
//                        VkInstance instance;
//                        VkResult result = vkCreateInstance(pCreateInfo, null, &instance);

//                        if (result != VkResult.Success)
//                        {
//                            Console.WriteLine($"Failed to create Vulkan instance: {result}");
//                            return;
//                        }

//                        Console.WriteLine("Vulkan instance created successfully.");

//                        // Clean up Vulkan instance
//                        vkDestroyInstance(instance, null);
//                    }

//                    // Free pinned layer name strings
//                    for (int i = 0; i < layers.Length; i++)
//                    {
//                        layerHandles[i].Free();
//                    }
//                }
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine($"Exception occurred: {ex.Message}");
//            }
//        }


//        // Helper method to create Vulkan version numbers
//        static VkVersion VkMakeVersion(uint major, uint minor, uint patch)
//        {
//            uint versionValue = (major << 22) | (minor << 12) | patch;
//            return new VkVersion(versionValue);
//        }
//    }
//}
// Copyright (c) Amer Koleci and Contributors.
// Licensed under the MIT License (MIT). See LICENSE in the repository root for more information.

using System.Numerics;
using Vortice.Vulkan;
using static System.Net.Mime.MediaTypeNames;
using static Vortice.Vulkan.Vulkan;

namespace ClearScreen;

public static unsafe class Program
{
#if DEBUG
    private static bool EnableValidationLayers = true;
#else
		private static bool EnableValidationLayers = false;
#endif
    public static void Main()
    {
        using TestApp testApp = new TestApp();
        testApp.Run();
    }

    class TestApp : Application
    {
        private GraphicsDevice? _graphicsDevice;
        private float _green = 0.0f;
        public override string Name => "01-ClearScreen";

        protected override void Initialize()
        {
            _graphicsDevice = new GraphicsDevice(Name, EnableValidationLayers, MainWindow);
        }

        public override void Dispose()
        {
            _graphicsDevice!.Dispose();

            base.Dispose();
        }

        protected override void OnTick()
        {
            _graphicsDevice!.RenderFrame(OnDraw);
        }

        private void OnDraw(VkCommandBuffer commandBuffer, VkFramebuffer framebuffer, VkExtent2D size)
        {
            float g = _green + 0.001f;
            if (g > 1.0f)
                g = 0.0f;
            _green = g;

            VkClearValue clearValue = new(1.0f, _green, 0.0f, 1.0f);

            // Begin the render pass.
            VkRenderPassBeginInfo renderPassBeginInfo = new()
            {
                renderPass = _graphicsDevice!.Swapchain.RenderPass,
                framebuffer = framebuffer,
                renderArea = new VkRect2D(VkOffset2D.Zero, size),
                clearValueCount = 1,
                pClearValues = &clearValue
            };
            vkCmdBeginRenderPass(commandBuffer, &renderPassBeginInfo, VkSubpassContents.Inline);
            vkCmdSetBlendConstants(commandBuffer, new Vector4(1.0f, 1.0f, 1.0f, 1.0f));
            vkCmdEndRenderPass(commandBuffer);
        }
    }
}

