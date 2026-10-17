// Copyright (c) Amer Koleci and Contributors.
// Licensed under the MIT License (MIT). See THIRD-PARTY-NOTICES.md, section 4.
// This file comes from the Vortice.Vulkan sample framework. TASK-0027 moved it
// into this project and changed it. ADR-0017 gives the rules for this project.

using Vortice.Vulkan;

namespace ThreeDEngine.Vulkan;

// The presentation to a window (ADR-0025 item 2): the surface, the render pass, the
// swapchain, the fence, the command pool and the semaphores of each frame, the acquire
// of an image and its present. TASK-0076 moved it out of GraphicsDevice, so that a
// device can exist with no window. The device creates it and disposes it.
//
// The swapchain follows the window (finding V8 of the codebase review of 2026-10-13,
// TASK-0074). A frame is skipped while the surface has no area, for example when the
// window is minimised. The swapchain, its framebuffers and its depth image are made
// again when the extent of the surface changes, or when an acquire or a present says
// that the swapchain is out of date or suboptimal. The render pass stays, so each
// pipeline that uses it stays valid. Until TASK-0074 the swapchain kept the extent of
// its creation, and each frame after a minimise could fail with no message.
public sealed unsafe class Presenter : IDisposable
{
    private readonly GraphicsDevice _device;
    private readonly VkSurfaceKHR _surface;
    private readonly Window _window;
    private readonly VkQueue _presentQueue;
    private readonly VkSurfaceFormatKHR _format;
    private readonly List<VkSemaphore> _recycledSemaphores = new();
    private PerFrame[] _perFrame;
    private uint _frameIndex;

    internal Presenter(GraphicsDevice device, VkSurfaceKHR surface, Window window, VkQueue presentQueue)
    {
        _device = device;
        _surface = surface;
        _window = window;
        _presentQueue = presentQueue;

        _format = Swapchain.ChooseSwapSurfaceFormat(Utils.QuerySwapChainSupport(device.InstanceApi, device.PhysicalDevice, surface).Formats);
        RenderPass = RenderPasses.Create(device, _format.format, VkImageLayout.PresentSrcKHR);
        Swapchain = new Swapchain(device, surface, window, RenderPass, _format, VkSwapchainKHR.Null);
        _perFrame = CreatePerFrame(Swapchain.ImageCount);
    }

    public Swapchain Swapchain { get; private set; }

    // The render pass of the swapchain, for the pipeline of the window. It does not
    // change when the swapchain is made again.
    public VkRenderPass RenderPass { get; }

    // How many times the swapchain was made again.
    public int Recreations { get; private set; }

    // The caller waits until the device is idle.
    public void Dispose()
    {
        var api = _device.DeviceApi;
        DestroyPerFrame();
        Swapchain.Dispose();
        api.vkDestroyRenderPass(RenderPass);

        foreach (VkSemaphore semaphore in _recycledSemaphores)
        {
            api.vkDestroySemaphore(semaphore);
        }
        _recycledSemaphores.Clear();

        _device.InstanceApi.vkDestroySurfaceKHR(_surface);
    }

    public void RenderFrame(Action<VkCommandBuffer, VkFramebuffer, VkExtent2D> draw)
    {
        var api = _device.DeviceApi;

        var extent = SurfaceExtent();
        if (extent.width == 0 || extent.height == 0)
            return;

        if (extent.width != Swapchain.Extent.width || extent.height != Swapchain.Extent.height)
            Recreate(extent);

        VkResult result = AcquireNextImage(out _frameIndex);
        if (result == VkResult.ErrorOutOfDateKHR)
        {
            Recreate(extent);
            return;
        }

        if (result != VkResult.Success && result != VkResult.SuboptimalKHR)
        {
            Log.Error($"Failed to acquire a swapchain image: {result}.");
            return;
        }

        // Begin command recording
        VkCommandBuffer cmd = _perFrame[_frameIndex].PrimaryCommandBuffer;

        VkCommandBufferBeginInfo beginInfo = new()
        {
            flags = VkCommandBufferUsageFlags.OneTimeSubmit
        };
        api.vkBeginCommandBuffer(cmd, &beginInfo).CheckResult();

        draw(cmd, Swapchain.Framebuffers[_frameIndex], Swapchain.Extent);

        // Complete the command buffer.
        api.vkEndCommandBuffer(cmd).CheckResult();

        if (_perFrame[_frameIndex].SwapchainReleaseSemaphore == VkSemaphore.Null)
        {
            api.vkCreateSemaphore(out _perFrame[_frameIndex].SwapchainReleaseSemaphore).CheckResult();
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
        api.vkQueueSubmit(_device.GraphicsQueue, submitInfo, _perFrame[_frameIndex].QueueSubmitFence);

        result = PresentImage(_frameIndex);

        if (result == VkResult.SuboptimalKHR || result == VkResult.ErrorOutOfDateKHR)
        {
            Recreate(SurfaceExtent());
        }
        else if (result != VkResult.Success)
        {
            Log.Error($"Failed to present swapchain image: {result}.");
        }
    }

    // The extent that the surface has now; (0, 0) while the window is minimised. A
    // surface that leaves the extent to the swapchain gives 0xFFFFFFFF, and then the
    // size of the window applies.
    private VkExtent2D SurfaceExtent()
    {
        _device.InstanceApi.vkGetPhysicalDeviceSurfaceCapabilitiesKHR(_device.PhysicalDevice, _surface, out VkSurfaceCapabilitiesKHR capabilities).CheckResult();
        return capabilities.currentExtent.width == uint.MaxValue ? _window.Extent : capabilities.currentExtent;
    }

    // Makes the swapchain, its framebuffers, its depth image and the resources of each
    // frame again, with the same render pass. A surface with no area keeps the old
    // swapchain until the next frame.
    private void Recreate(VkExtent2D extent)
    {
        if (extent.width == 0 || extent.height == 0)
            return;

        _device.WaitIdle();
        DestroyPerFrame();
        var old = Swapchain;
        Swapchain = new Swapchain(_device, _surface, _window, RenderPass, _format, old.Handle);
        old.Dispose();
        _perFrame = CreatePerFrame(Swapchain.ImageCount);
        Recreations++;
        Log.Info($"The swapchain is made again with the extent {Swapchain.Extent.width} x {Swapchain.Extent.height}.");
    }

    private PerFrame[] CreatePerFrame(int count)
    {
        var api = _device.DeviceApi;
        var perFrame = new PerFrame[count];
        for (int i = 0; i < perFrame.Length; i++)
        {
            api.vkCreateFence(VkFenceCreateFlags.Signaled, out perFrame[i].QueueSubmitFence).CheckResult();

            VkCommandPoolCreateInfo poolCreateInfo = new()
            {
                flags = VkCommandPoolCreateFlags.Transient,
                queueFamilyIndex = _device.GraphicsFamily,
            };
            api.vkCreateCommandPool(&poolCreateInfo, null, out perFrame[i].PrimaryCommandPool).CheckResult();

            api.vkAllocateCommandBuffer(perFrame[i].PrimaryCommandPool, out perFrame[i].PrimaryCommandBuffer).CheckResult();
        }

        return perFrame;
    }

    // The caller waits until the device is idle.
    private void DestroyPerFrame()
    {
        var api = _device.DeviceApi;
        for (var i = 0; i < _perFrame.Length; i++)
        {
            api.vkDestroyFence(_perFrame[i].QueueSubmitFence);

            if (_perFrame[i].PrimaryCommandBuffer != IntPtr.Zero)
            {
                api.vkFreeCommandBuffers(_perFrame[i].PrimaryCommandPool, _perFrame[i].PrimaryCommandBuffer);

                _perFrame[i].PrimaryCommandBuffer = IntPtr.Zero;
            }

            api.vkDestroyCommandPool(_perFrame[i].PrimaryCommandPool);

            if (_perFrame[i].SwapchainAcquireSemaphore != VkSemaphore.Null)
            {
                api.vkDestroySemaphore(_perFrame[i].SwapchainAcquireSemaphore);
                _perFrame[i].SwapchainAcquireSemaphore = VkSemaphore.Null;
            }

            if (_perFrame[i].SwapchainReleaseSemaphore != VkSemaphore.Null)
            {
                api.vkDestroySemaphore(_perFrame[i].SwapchainReleaseSemaphore);
                _perFrame[i].SwapchainReleaseSemaphore = VkSemaphore.Null;
            }
        }

        _perFrame = [];
    }

    // An acquire that gives SUBOPTIMAL gives an image too, and its semaphore will be
    // signalled; only a failure recycles the semaphore.
    private VkResult AcquireNextImage(out uint imageIndex)
    {
        var api = _device.DeviceApi;
        VkSemaphore acquireSemaphore;
        if (_recycledSemaphores.Count == 0)
        {
            api.vkCreateSemaphore(out acquireSemaphore).CheckResult();
        }
        else
        {
            acquireSemaphore = _recycledSemaphores[_recycledSemaphores.Count - 1];
            _recycledSemaphores.RemoveAt(_recycledSemaphores.Count - 1);
        }

        VkResult result = api.vkAcquireNextImageKHR(Swapchain.Handle, ulong.MaxValue, acquireSemaphore, VkFence.Null, out imageIndex);

        if (result != VkResult.Success && result != VkResult.SuboptimalKHR)
        {
            _recycledSemaphores.Add(acquireSemaphore);
            return result;
        }

        if (_perFrame[imageIndex].QueueSubmitFence != VkFence.Null)
        {
            api.vkWaitForFences(_perFrame[imageIndex].QueueSubmitFence, true, ulong.MaxValue);
            api.vkResetFences(_perFrame[imageIndex].QueueSubmitFence);
        }

        if (_perFrame[imageIndex].PrimaryCommandPool != VkCommandPool.Null)
        {
            api.vkResetCommandPool(_perFrame[imageIndex].PrimaryCommandPool, VkCommandPoolResetFlags.None);
        }

        // Recycle the old semaphore back into the semaphore manager.
        VkSemaphore oldSemaphore = _perFrame[imageIndex].SwapchainAcquireSemaphore;

        if (oldSemaphore != VkSemaphore.Null)
        {
            _recycledSemaphores.Add(oldSemaphore);
        }

        _perFrame[imageIndex].SwapchainAcquireSemaphore = acquireSemaphore;

        return result;
    }

    private VkResult PresentImage(uint imageIndex)
    {
        return _device.DeviceApi.vkQueuePresentKHR(_presentQueue, _perFrame[imageIndex].SwapchainReleaseSemaphore, Swapchain.Handle, imageIndex);
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
