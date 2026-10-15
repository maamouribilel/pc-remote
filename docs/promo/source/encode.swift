// Muxes PNG frames + a WAV into an H.264/AAC MP4 with AVFoundation (no ffmpeg needed).
//   swiftc -O encode.swift -o encode
//   ./encode <framesDir> <fps> <soundtrack.wav> <out.mp4>
import AVFoundation
import CoreGraphics
import Foundation
import ImageIO

let args = CommandLine.arguments
guard args.count == 5, let fps = Int32(args[2]) else {
    FileHandle.standardError.write("usage: encode <framesDir> <fps> <audio.wav> <out.mp4>\n".data(using: .utf8)!)
    exit(2)
}
let framesDir = URL(fileURLWithPath: args[1])
let wavURL = URL(fileURLWithPath: args[3])
let outURL = URL(fileURLWithPath: args[4])

let files = try FileManager.default.contentsOfDirectory(atPath: framesDir.path)
    .filter { $0.hasPrefix("frame_") && $0.hasSuffix(".png") }
    .sorted()
func loadImage(_ name: String) -> CGImage {
    let src = CGImageSourceCreateWithURL(framesDir.appendingPathComponent(name) as CFURL, nil)!
    return CGImageSourceCreateImageAtIndex(src, 0, nil)!
}
guard let first = files.first else { fatalError("no frame_*.png in \(framesDir.path)") }
let probe = loadImage(first)
let width = probe.width, height = probe.height

try? FileManager.default.removeItem(at: outURL)
let writer = try AVAssetWriter(outputURL: outURL, fileType: .mp4)
writer.shouldOptimizeForNetworkUse = true

let video = AVAssetWriterInput(mediaType: .video, outputSettings: [
    AVVideoCodecKey: AVVideoCodecType.h264,
    AVVideoWidthKey: width,
    AVVideoHeightKey: height,
    AVVideoColorPropertiesKey: [
        AVVideoColorPrimariesKey: AVVideoColorPrimaries_ITU_R_709_2,
        AVVideoTransferFunctionKey: AVVideoTransferFunction_ITU_R_709_2,
        AVVideoYCbCrMatrixKey: AVVideoYCbCrMatrix_ITU_R_709_2,
    ],
    AVVideoCompressionPropertiesKey: [
        AVVideoAverageBitRateKey: 14_000_000,
        AVVideoProfileLevelKey: AVVideoProfileLevelH264HighAutoLevel,
        AVVideoMaxKeyFrameIntervalKey: Int(fps) * 2,
        AVVideoExpectedSourceFrameRateKey: Int(fps),
    ],
])
video.expectsMediaDataInRealTime = false
let adaptor = AVAssetWriterInputPixelBufferAdaptor(assetWriterInput: video, sourcePixelBufferAttributes: [
    kCVPixelBufferPixelFormatTypeKey as String: kCVPixelFormatType_32BGRA,
    kCVPixelBufferWidthKey as String: width,
    kCVPixelBufferHeightKey as String: height,
])
writer.add(video)

let asset = AVURLAsset(url: wavURL)
guard let track = asset.tracks(withMediaType: .audio).first else { fatalError("no audio track in \(wavURL.path)") }
let reader = try AVAssetReader(asset: asset)
let pcm = AVAssetReaderTrackOutput(track: track, outputSettings: [
    AVFormatIDKey: kAudioFormatLinearPCM,
    AVLinearPCMBitDepthKey: 16,
    AVLinearPCMIsFloatKey: false,
    AVLinearPCMIsBigEndianKey: false,
    AVLinearPCMIsNonInterleaved: false,
    AVSampleRateKey: 48_000,
    AVNumberOfChannelsKey: 2,
])
reader.add(pcm)
var layout = AudioChannelLayout()
layout.mChannelLayoutTag = kAudioChannelLayoutTag_Stereo
let audio = AVAssetWriterInput(mediaType: .audio, outputSettings: [
    AVFormatIDKey: kAudioFormatMPEG4AAC,
    AVSampleRateKey: 48_000,
    AVNumberOfChannelsKey: 2,
    AVEncoderBitRateKey: 256_000,
    AVChannelLayoutKey: Data(bytes: &layout, count: MemoryLayout<AudioChannelLayout>.size),
])
audio.expectsMediaDataInRealTime = false
writer.add(audio)

guard writer.startWriting(), reader.startReading() else {
    fatalError("start failed: \(String(describing: writer.error ?? reader.error))")
}
writer.startSession(atSourceTime: .zero)

let srgb = CGColorSpace(name: CGColorSpace.sRGB)!
let group = DispatchGroup()
var index = 0
group.enter()
video.requestMediaDataWhenReady(on: DispatchQueue(label: "video")) {
    while video.isReadyForMoreMediaData {
        if index >= files.count {
            video.markAsFinished()
            group.leave()
            return
        }
        autoreleasepool {
            let image = loadImage(files[index])
            var buffer: CVPixelBuffer?
            CVPixelBufferPoolCreatePixelBuffer(nil, adaptor.pixelBufferPool!, &buffer)
            let pb = buffer!
            CVPixelBufferLockBaseAddress(pb, [])
            let ctx = CGContext(
                data: CVPixelBufferGetBaseAddress(pb), width: width, height: height, bitsPerComponent: 8,
                bytesPerRow: CVPixelBufferGetBytesPerRow(pb), space: srgb,
                bitmapInfo: CGImageAlphaInfo.premultipliedFirst.rawValue | CGBitmapInfo.byteOrder32Little.rawValue)!
            ctx.draw(image, in: CGRect(x: 0, y: 0, width: width, height: height))
            CVPixelBufferUnlockBaseAddress(pb, [])
            CVBufferSetAttachment(pb, kCVImageBufferColorPrimariesKey, kCVImageBufferColorPrimaries_ITU_R_709_2, .shouldPropagate)
            CVBufferSetAttachment(pb, kCVImageBufferTransferFunctionKey, kCVImageBufferTransferFunction_ITU_R_709_2, .shouldPropagate)
            CVBufferSetAttachment(pb, kCVImageBufferYCbCrMatrixKey, kCVImageBufferYCbCrMatrix_ITU_R_709_2, .shouldPropagate)
            if !adaptor.append(pb, withPresentationTime: CMTime(value: CMTimeValue(index), timescale: fps)) {
                fatalError("append frame \(index) failed: \(String(describing: writer.error))")
            }
            index += 1
        }
    }
}
group.enter()
audio.requestMediaDataWhenReady(on: DispatchQueue(label: "audio")) {
    while audio.isReadyForMoreMediaData {
        if let sample = pcm.copyNextSampleBuffer() {
            audio.append(sample)
        } else {
            audio.markAsFinished()
            group.leave()
            return
        }
    }
}
group.wait()
let done = DispatchSemaphore(value: 0)
writer.finishWriting { done.signal() }
done.wait()
guard writer.status == .completed else { fatalError("writing failed: \(String(describing: writer.error))") }
print("wrote \(outURL.lastPathComponent): \(files.count) frames @ \(fps) fps, \(width)x\(height)")
