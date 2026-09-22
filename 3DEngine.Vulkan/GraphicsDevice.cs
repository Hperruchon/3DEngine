// Copyright (c) Amer Koleci and Contributors.
// Licensed under the MIT License (MIT). See THIRD-PARTY-NOTICES.md, section 4.
// This file comes from the Vortice.Vulkan sample framework. TASK-0027 moved it
// into this project and changed it. ADR-0017 gives the rules for this project.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Vortice.Vulkan;
using static SDL3.SDL3;
using static Vortice.Vulkan.Vulkan;

namespace ThreeDEngine.Vulkan;

public unsafe sealed class GraphicsDevice : IDisposable
{
    public readonly bool DebugUtils;
    public readonly VkInstance VkInstance;

    private readonly VkDebugUtilsMessengerEXT _debugMessenger = VkDebugUtilsMessengerEXT.Null;
    public readonly VkPhysicalDevice PhysicalDevice;
    public readonly VkDevice VkDevice;
    public readonly VkQueue GraphicsQueue;
    public readonly VkQueue PresentQueue;
    public readonly Swapchain Swapchain;
    private PerFrame[] _perFrame;
    private uint _frameIndex;

    private readonly List<VkSemaphore> _recycledSemaphores = new();

    public GraphicsDevice(string applicationName, bool enableValidation, Window window)
    {
        HashSet<VkUtf8String> availableInstanceLayers = new(EnumerateInstanceLayers());
        HashSet<VkUtf8String> availableInstanceExtensions = new(GetInstanceExtensions());

        List<VkUtf8String> instanceExtensions = [];
        foreach (string sdlExt in SDL_Vulkan_GetInstanceExtensions())
        {
            ReadOnlySpan<byte> sdlExtSpan = Encoding.UTF8.GetBytes(sdlExt);
            instanceExtensions.Add(sdlExtSpan);
        }

        List<VkUtf8String> instanceLayers = [];

        if (enableValidation)
        {
            // Determine the optimal validation layers to enable that are necessary for useful debugging
            GetOptimalValidationLayers(availableInstanceLayers, instanceLayers);
        }

        // Check if VK_EXT_debug_utils is supported, which supersedes VK_EXT_Debug_Report
        foreach (VkUtf8String availableExtension in availableInstanceExtensions)
        {
            if (availableExtension == VK_EXT_DEBUG_UTILS_EXTENSION_NAME)
            {
                DebugUtils = true;
                instanceExtensions.Add(VK_EXT_DEBUG_UTILS_EXTENSION_NAME);
            }
            else if (availableExtension == VK_EXT_SWAPCHAIN_COLOR_SPACE_EXTENSION_NAME)
            {
                instanceExtensions.Add(VK_EXT_SWAPCHAIN_COLOR_SPACE_EXTENSION_NAME);
            }
        }

        VkUtf8ReadOnlyString pApplicationName = Encoding.UTF8.GetBytes(applicationName);
        VkUtf8ReadOnlyString pEngineName = "Vortice"u8;

        VkApplicationInfo appInfo = new()
        {
            pApplicationName = pApplicationName,
            applicationVersion = new VkVersion(1, 0, 0),
            pEngineName = pEngineName,
            engineVersion = new VkVersion(1, 0, 0),
            apiVersion = VkVersion.Version_1_3
        };

        using VkStringArray vkLayerNames = new(instanceLayers);
        using VkStringArray vkInstanceExtensions = new(instanceExtensions);

        VkInstanceCreateInfo instanceCreateInfo = new()
        {
            pApplicationInfo = &appInfo,
            enabledLayerCount = vkLayerNames.Length,
            ppEnabledLayerNames = vkLayerNames,
            enabledExtensionCount = vkInstanceExtensions.Length,
            ppEnabledExtensionNames = vkInstanceExtensions
        };

        VkDebugUtilsMessengerCreateInfoEXT debugUtilsCreateInfo = new();

        if (instanceLayers.Count > 0)
        {
            debugUtilsCreateInfo.messageSeverity = VkDebugUtilsMessageSeverityFlagsEXT.Error | VkDebugUtilsMessageSeverityFlagsEXT.Warning;
            debugUtilsCreateInfo.messageType = VkDebugUtilsMessageTypeFlagsEXT.Validation | VkDebugUtilsMessageTypeFlagsEXT.Performance;
            debugUtilsCreateInfo.pfnUserCallback = &DebugMessengerCallback;
            instanceCreateInfo.pNext = &debugUtilsCreateInfo;
        }

        VkResult result = vkCreateInstance(&instanceCreateInfo, null, out VkInstance);
        if (result != VkResult.Success)
        {
            throw new InvalidOperationException($"Failed to create vulkan instance: {result}");
        }

        vkLoadInstanceOnly(VkInstance);

        if (instanceLayers.Count > 0)
        {
            vkCreateDebugUtilsMessengerEXT(VkInstance, &debugUtilsCreateInfo, null, out _debugMessenger).CheckResult();
        }

        Log.Info($"Created VkInstance with version: {appInfo.apiVersion.Major}.{appInfo.apiVersion.Minor}.{appInfo.apiVersion.Patch}");
        if (instanceLayers.Count > 0)
        {
            foreach (var layer in instanceLayers)
            {
                Log.Info($"Instance layer '{layer}'");
            }
        }

        foreach (VkUtf8String extension in instanceExtensions)
        {
            Log.Info($"Instance extension '{extension}'");
        }

        VkSurfaceKHR surface = window.CreateSurface(VkInstance);

        // Find physical device, setup queue's and create device.
        uint physicalDevicesCount = 0;
        vkEnumeratePhysicalDevices(VkInstance, &physicalDevicesCount, null).CheckResult();

        if (physicalDevicesCount == 0)
        {
            throw new Exception("Vulkan: Failed to find GPUs with Vulkan support");
        }

        VkPhysicalDevice* physicalDevices = stackalloc VkPhysicalDevice[(int)physicalDevicesCount];
        vkEnumeratePhysicalDevices(VkInstance, &physicalDevicesCount, physicalDevices).CheckResult();

        for (int i = 0; i < physicalDevicesCount; i++)
        {
            VkPhysicalDevice physicalDevice = physicalDevices[i];

            if (IsDeviceSuitable(physicalDevice, surface) == false)
                continue;

            vkGetPhysicalDeviceProperties(physicalDevice, out VkPhysicalDeviceProperties checkProperties);
            bool discrete = checkProperties.deviceType == VkPhysicalDeviceType.DiscreteGpu;

            if (discrete || PhysicalDevice.IsNull)
            {
                PhysicalDevice = physicalDevice;
                if (discrete)
                {
                    // If this is discrete GPU, look no further (prioritize discrete GPU)
                    break;
                }
            }
        }

        var queueFamilies = FindQueueFamilies(PhysicalDevice, surface);

        HashSet<uint> uniqueQueueFamilies = new();
        uniqueQueueFamilies.Add(queueFamilies.graphicsFamily);
        uniqueQueueFamilies.Add(queueFamilies.presentFamily);

        float priority = 1.0f;
        uint queueCount = 0;
        VkDeviceQueueCreateInfo* queueCreateInfos = stackalloc VkDeviceQueueCreateInfo[2];

        foreach (uint queueFamily in uniqueQueueFamilies)
        {
            queueCreateInfos[queueCount++] = new VkDeviceQueueCreateInfo
            {
                queueFamilyIndex = queueFamily,
                queueCount = 1,
                pQueuePriorities = &priority
            };
        }

        List<VkUtf8String> enabledExtensions = new()
        {
            VK_KHR_SWAPCHAIN_EXTENSION_NAME
        };

        using var deviceExtensionNames = new VkStringArray(enabledExtensions);

        VkDeviceCreateInfo deviceCreateInfo = new()
        {
            queueCreateInfoCount = queueCount,
            pQueueCreateInfos = queueCreateInfos,
            enabledExtensionCount = deviceExtensionNames.Length,
            ppEnabledExtensionNames = deviceExtensionNames,
            pEnabledFeatures = null,
        };

        result = vkCreateDevice(PhysicalDevice, &deviceCreateInfo, null, out VkDevice);
        if (result != VkResult.Success)
            throw new Exception($"Failed to create Vulkan Logical Device, {result}");

        vkLoadDevice(VkDevice);

        vkGetDeviceQueue(VkDevice, queueFamilies.graphicsFamily, 0, out GraphicsQueue);
        vkGetDeviceQueue(VkDevice, queueFamilies.presentFamily, 0, out PresentQueue);

        // Create swap chain
        Swapchain = new Swapchain(this, surface, window);
        _perFrame = new PerFrame[Swapchain.ImageCount];
        for (int i = 0; i < _perFrame.Length; i++)
        {
            vkCreateFence(VkDevice, VkFenceCreateFlags.Signaled, out _perFrame[i].QueueSubmitFence).CheckResult();

            VkCommandPoolCreateInfo poolCreateInfo = new()
            {
                flags = VkCommandPoolCreateFlags.Transient,
                queueFamilyIndex = queueFamilies.graphicsFamily,
            };
            vkCreateCommandPool(VkDevice, &poolCreateInfo, null, out _perFrame[i].PrimaryCommandPool).CheckResult();

            vkAllocateCommandBuffer(VkDevice, _perFrame[i].PrimaryCommandPool, out _perFrame[i].PrimaryCommandBuffer).CheckResult();
        }
    }

    public void Dispose()
    {
        // Don't release anything until the GPU is completely idle.
        WaitIdle();

        Swapchain.Dispose();

        for (var i = 0; i < _perFrame.Length; i++)
        {
            vkDestroyFence(VkDevice, _perFrame[i].QueueSubmitFence);

            if (_perFrame[i].PrimaryCommandBuffer != IntPtr.Zero)
            {
                vkFreeCommandBuffers(VkDevice, _perFrame[i].PrimaryCommandPool, _perFrame[i].PrimaryCommandBuffer);

                _perFrame[i].PrimaryCommandBuffer = IntPtr.Zero;
            }

            vkDestroyCommandPool(VkDevice, _perFrame[i].PrimaryCommandPool);

            if (_perFrame[i].SwapchainAcquireSemaphore != VkSemaphore.Null)
            {
                vkDestroySemaphore(VkDevice, _perFrame[i].SwapchainAcquireSemaphore);
                _perFrame[i].SwapchainAcquireSemaphore = VkSemaphore.Null;
            }

            if (_perFrame[i].SwapchainReleaseSemaphore != VkSemaphore.Null)
            {
                vkDestroySemaphore(VkDevice, _perFrame[i].SwapchainReleaseSemaphore);
                _perFrame[i].SwapchainReleaseSemaphore = VkSemaphore.Null;
            }
        }

        foreach (VkSemaphore semaphore in _recycledSemaphores)
        {
            vkDestroySemaphore(VkDevice, semaphore);
        }
        _recycledSemaphores.Clear();

        if (VkDevice.IsNotNull)
        {
            vkDestroyDevice(VkDevice);
        }

        if (_debugMessenger != VkDebugUtilsMessengerEXT.Null)
        {
            vkDestroyDebugUtilsMessengerEXT(VkInstance, _debugMessenger);
        }

        if (VkInstance != VkInstance.Null)
        {
            vkDestroyInstance(VkInstance);
        }
    }

    public void WaitIdle()
    {
        vkDeviceWaitIdle(VkDevice).CheckResult();
    }

    public void RenderFrame(
        Action<VkCommandBuffer, VkFramebuffer, VkExtent2D> draw,
        [CallerMemberName] string? frameName = null)
    {
        VkResult result = AcquireNextImage(out _frameIndex);

        // Handle outdated error in acquire.
        if (result == VkResult.SuboptimalKHR || result == VkResult.ErrorOutOfDateKHR)
        {
            //Resize(context.swapchain_dimensions.width, context.swapchain_dimensions.height);
            result = AcquireNextImage(out _frameIndex);
        }

        if (result != VkResult.Success)
        {
            vkDeviceWaitIdle(VkDevice);
            return;
        }

        // Begin command recording
        VkCommandBuffer cmd = _perFrame[_frameIndex].PrimaryCommandBuffer;

        VkCommandBufferBeginInfo beginInfo = new()
        {
            flags = VkCommandBufferUsageFlags.OneTimeSubmit
        };
        vkBeginCommandBuffer(cmd, &beginInfo).CheckResult();

        draw(cmd, Swapchain.Framebuffers[_frameIndex], Swapchain.Extent);

        // Complete the command buffer.
        vkEndCommandBuffer(cmd).CheckResult();

        if (_perFrame[_frameIndex].SwapchainReleaseSemaphore == VkSemaphore.Null)
        {
            vkCreateSemaphore(VkDevice, out _perFrame[_frameIndex].SwapchainReleaseSemaphore).CheckResult();
        }

        VkPipelineStageFlags wait_stage = VkPipelineStageFlags.ColorAttachmentOutput;
        VkSemaphore waitSemaphore = _perFrame[_frameIndex].SwapchainAcquireSemaphore;
        VkSemaphore signalSemaphore = _perFrame[_frameIndex].SwapchainReleaseSemaphore;

        VkSubmitInfo submitInfo = new()
        {
            commandBufferCount = 1u,
            pCommandBuffers = &cmd,
            waitSemaphoreCount = 1u,
            pWaitSemaphores = &waitSemaphore,
            pWaitDstStageMask = &wait_stage,
            signalSemaphoreCount = 1u,
            pSignalSemaphores = &signalSemaphore
        };

        // Submit command buffer to graphics queue
        vkQueueSubmit(GraphicsQueue, submitInfo, _perFrame[_frameIndex].QueueSubmitFence);

        result = PresentImage(_frameIndex);

        // Handle Outdated error in present.
        if (result == VkResult.SuboptimalKHR || result == VkResult.ErrorOutOfDateKHR)
        {
            //Resize(context.swapchain_dimensions.width, context.swapchain_dimensions.height);
        }
        else if (result != VkResult.Success)
        {
            Log.Error("Failed to present swapchain image.");
        }
    }

    private VkResult AcquireNextImage(out uint imageIndex)
    {
        VkSemaphore acquireSemaphore;
        if (_recycledSemaphores.Count == 0)
        {
            vkCreateSemaphore(VkDevice, out acquireSemaphore).CheckResult();
        }
        else
        {
            acquireSemaphore = _recycledSemaphores[_recycledSemaphores.Count - 1];
            _recycledSemaphores.RemoveAt(_recycledSemaphores.Count - 1);
        }

        VkResult result = vkAcquireNextImageKHR(VkDevice, Swapchain.Handle, ulong.MaxValue, acquireSemaphore, VkFence.Null, out imageIndex);

        if (result != VkResult.Success)
        {
            _recycledSemaphores.Add(acquireSemaphore);
            return result;
        }

        if (_perFrame[imageIndex].QueueSubmitFence != VkFence.Null)
        {
            vkWaitForFences(VkDevice, _perFrame[imageIndex].QueueSubmitFence, true, ulong.MaxValue);
            vkResetFences(VkDevice, _perFrame[imageIndex].QueueSubmitFence);
        }

        if (_perFrame[imageIndex].PrimaryCommandPool != VkCommandPool.Null)
        {
            vkResetCommandPool(VkDevice, _perFrame[imageIndex].PrimaryCommandPool, VkCommandPoolResetFlags.None);
        }

        // Recycle the old semaphore back into the semaphore manager.
        VkSemaphore oldSemaphore = _perFrame[imageIndex].SwapchainAcquireSemaphore;

        if (oldSemaphore != VkSemaphore.Null)
        {
            _recycledSemaphores.Add(oldSemaphore);
        }

        _perFrame[imageIndex].SwapchainAcquireSemaphore = acquireSemaphore;

        return VkResult.Success;
    }

    private VkResult PresentImage(uint imageIndex)
    {
        return vkQueuePresentKHR(PresentQueue, _perFrame[imageIndex].SwapchainReleaseSemaphore, Swapchain.Handle, imageIndex);
    }

    public static implicit operator VkDevice(GraphicsDevice device) => device.VkDevice;

    #region Private Methods
    private static void GetOptimalValidationLayers(
        HashSet<VkUtf8String> availableLayers,
        List<VkUtf8String> instanceLayers)
    {
        // The preferred validation layer is "VK_LAYER_KHRONOS_validation"
        List<VkUtf8String> validationLayers =
        [
            "VK_LAYER_KHRONOS_validation"u8
        ];

        if (ValidateLayers(availableLayers, validationLayers))
        {
            instanceLayers.AddRange(validationLayers);
            return;
        }

        // Otherwise we fallback to using the LunarG meta layer
        validationLayers =
        [
            "VK_LAYER_LUNARG_standard_validation"u8
        ];

        if (ValidateLayers(availableLayers, validationLayers))
        {
            instanceLayers.AddRange(validationLayers);
            return;
        }

        // Otherwise we attempt to enable the individual layers that compose the LunarG meta layer since it doesn't exist
        validationLayers = new()
        {
            "VK_LAYER_GOOGLE_threading"u8,
            "VK_LAYER_LUNARG_parameter_validation"u8,
            "VK_LAYER_LUNARG_object_tracker"u8,
            "VK_LAYER_LUNARG_core_validation"u8,
            "VK_LAYER_GOOGLE_unique_objects"u8,
        };

        if (ValidateLayers(availableLayers, validationLayers))
        {
            instanceLayers.AddRange(validationLayers);
            return;
        }

        // Otherwise as a last resort we fallback to attempting to enable the LunarG core layer
        validationLayers = new()
        {
            "VK_LAYER_LUNARG_core_validation"u8
        };

        if (ValidateLayers(availableLayers, validationLayers))
        {
            instanceLayers.AddRange(validationLayers);
            return;
        }
    }

    private static bool ValidateLayers(
        HashSet<VkUtf8String> availableLayers,
        List<VkUtf8String> required
        )
    {
        foreach (VkUtf8String layer in required)
        {
            bool found = false;
            foreach (VkUtf8String availableLayer in availableLayers)
            {
                if (availableLayer == layer)
                {
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                //Log.Warn("Validation Layer '{}' not found", layer);
                return false;
            }
        }

        return true;
    }

    private static bool IsDeviceSuitable(VkPhysicalDevice physicalDevice, VkSurfaceKHR surface)
    {
        var checkQueueFamilies = FindQueueFamilies(physicalDevice, surface);
        if (checkQueueFamilies.graphicsFamily == VK_QUEUE_FAMILY_IGNORED)
            return false;

        if (checkQueueFamilies.presentFamily == VK_QUEUE_FAMILY_IGNORED)
            return false;

        SwapChainSupportDetails swapChainSupport = Utils.QuerySwapChainSupport(physicalDevice, surface);
        return !swapChainSupport.Formats.IsEmpty && !swapChainSupport.PresentModes.IsEmpty;
    }

    [UnmanagedCallersOnly]
    private static uint DebugMessengerCallback(VkDebugUtilsMessageSeverityFlagsEXT messageSeverity,
        VkDebugUtilsMessageTypeFlagsEXT messageTypes,
        VkDebugUtilsMessengerCallbackDataEXT* pCallbackData,
        void* userData)
    {
        VkUtf8String message = new VkUtf8String(pCallbackData->pMessage)!;
        if (messageTypes == VkDebugUtilsMessageTypeFlagsEXT.Validation)
        {
            if (messageSeverity == VkDebugUtilsMessageSeverityFlagsEXT.Error)
            {
                Log.Error($"[Vulkan]: Validation: {messageSeverity} - {message}");
            }
            else if (messageSeverity == VkDebugUtilsMessageSeverityFlagsEXT.Warning)
            {
                Log.Warn($"[Vulkan]: Validation: {messageSeverity} - {message}");
            }

            Debug.WriteLine($"[Vulkan]: Validation: {messageSeverity} - {message}");
        }
        else
        {
            if (messageSeverity == VkDebugUtilsMessageSeverityFlagsEXT.Error)
            {
                Log.Error($"[Vulkan]: {messageSeverity} - {message}");
            }
            else if (messageSeverity == VkDebugUtilsMessageSeverityFlagsEXT.Warning)
            {
                Log.Warn($"[Vulkan]: {messageSeverity} - {message}");
            }

            Debug.WriteLine($"[Vulkan]: {messageSeverity} - {message}");
        }

        return VK_FALSE;
    }

    static (uint graphicsFamily, uint presentFamily) FindQueueFamilies(
        VkPhysicalDevice device, VkSurfaceKHR surface)
    {
        ReadOnlySpan<VkQueueFamilyProperties> queueFamilies = vkGetPhysicalDeviceQueueFamilyProperties(device);

        uint graphicsFamily = VK_QUEUE_FAMILY_IGNORED;
        uint presentFamily = VK_QUEUE_FAMILY_IGNORED;
        uint i = 0;
        foreach (VkQueueFamilyProperties queueFamily in queueFamilies)
        {
            if ((queueFamily.queueFlags & VkQueueFlags.Graphics) != VkQueueFlags.None)
            {
                graphicsFamily = i;
            }

            vkGetPhysicalDeviceSurfaceSupportKHR(device, i, surface, out VkBool32 presentSupport);
            if (presentSupport)
            {
                presentFamily = i;
            }

            if (graphicsFamily != VK_QUEUE_FAMILY_IGNORED
                && presentFamily != VK_QUEUE_FAMILY_IGNORED)
            {
                break;
            }

            i++;
        }

        return (graphicsFamily, presentFamily);
    }
    #endregion

    private static readonly Lazy<bool> s_isSupported = new(CheckIsSupported);

    public static bool IsSupported() => s_isSupported.Value;

    private static bool CheckIsSupported()
    {
        try
        {
            VkResult result = vkInitialize();
            if (result != VkResult.Success)
                return false;

            uint propCount;
            result = vkEnumerateInstanceExtensionProperties(&propCount, null);
            if (result != VkResult.Success)
            {
                return false;
            }

            // We require Vulkan 1.1 or higher
            VkVersion version = vkEnumerateInstanceVersion();
            if (version < VkVersion.Version_1_1)
                return false;

            return true;
        }
        catch
        {
            return false;
        }
    }

    private unsafe static VkUtf8String[] EnumerateInstanceLayers()
    {
        if (!IsSupported())
        {
            return [];
        }

        uint count = 0;
        VkResult result = vkEnumerateInstanceLayerProperties(&count, null);
        if (result != VkResult.Success || count == 0)
        {
            return [];
        }

        VkLayerProperties[] props = new VkLayerProperties[(int)count];
        vkEnumerateInstanceLayerProperties(props).CheckResult();

        VkUtf8String[] resultExt = new VkUtf8String[count];
        for (int i = 0; i < count; i++)
        {
            fixed (byte* pLayerName = props[i].layerName)
            {
                resultExt[i] = new VkUtf8String(pLayerName);
            }
        }

        return resultExt;
    }

    private static VkUtf8String[] GetInstanceExtensions()
    {
        uint count = 0;
        VkResult result = vkEnumerateInstanceExtensionProperties(&count, null);
        if (result != VkResult.Success)
        {
            return [];
        }

        if (count == 0)
        {
            return [];
        }

        VkExtensionProperties[] props = new VkExtensionProperties[(int)count];
        vkEnumerateInstanceExtensionProperties(props);

        VkUtf8String[] extensions = new VkUtf8String[count];
        for (int i = 0; i < count; i++)
        {
            fixed (byte* pExtensionName = props[i].extensionName)
            {
                extensions[i] = new VkUtf8String(pExtensionName);
            }
        }

        return extensions;
    }

    private struct PerFrame
    {
        public VkFence QueueSubmitFence;
        public VkCommandPool PrimaryCommandPool;
        public VkCommandBuffer PrimaryCommandBuffer;
        public VkSemaphore SwapchainAcquireSemaphore;
        public VkSemaphore SwapchainReleaseSemaphore;
    }
}
