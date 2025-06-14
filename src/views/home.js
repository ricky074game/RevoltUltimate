import React from 'react'
import { Link } from 'react-router-dom'

import Script from 'dangerous-html/react'
import { Helmet } from 'react-helmet'

import Announcement from '../components/announcement'
import Navbar from '../components/navbar'
import Highlight from '../components/highlight'
import Point from '../components/point'
import Accordion from '../components/accordion'
import Feature from '../components/feature'
import Check from '../components/check'
import Quote from '../components/quote'
import Footer from '../components/footer'
import './home.css'

const Home = (props) => {
  return (
    <div className="home-container1">
      <Helmet>
        <title>Finbest</title>
        <meta name="description" content="Description of the website" />
        <meta property="og:title" content="Finbest" />
        <meta property="og:description" content="Description of the website" />
      </Helmet>
      <div className="home-hero">
        <header className="home-heading10">
          <div id="notifcation" className="home-notification">
            <Link to="/">
              <Announcement
                rootClassName="announcementroot-class-name"
                className="home-component10"
              ></Announcement>
            </Link>
          </div>
          <Navbar></Navbar>
        </header>
        <div className="home-content10">
          <div className="home-content11">
            <h1 className="home-title1">
              finbest is a clean, easy to use, finance app.
            </h1>
            <span className="home-caption1">
              Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do
              eiusmod tempor incididunt.
            </span>
            <div className="home-hero-buttons1">
              <div className="home-ios-btn1">
                <img
                  alt="pastedImage"
                  src="/pastedimage-zmzg.svg"
                  className="home-apple1"
                />
                <span className="home-caption2">Download for iOS</span>
              </div>
              <div className="home-android-btn1">
                <img
                  alt="pastedImage"
                  src="/pastedimage-ld65.svg"
                  className="home-android1"
                />
                <span className="home-caption3">Download for Android</span>
              </div>
            </div>
          </div>
          <div className="home-images">
            <div className="home-column1">
              <img
                alt="pastedImage"
                src="/pastedimage-oy26-1500h.png"
                className="home-pasted-image1"
              />
            </div>
            <div className="home-column2">
              <img
                alt="pastedImage"
                src="/pastedimage-v31-1500h.png"
                className="home-pasted-image2"
              />
              <img
                alt="pastedImage"
                src="/pastedimage-c39.svg"
                className="home-pasted-image3"
              />
            </div>
            <div className="home-column3">
              <img
                alt="pastedImage"
                src="/pastedimage-iqnj.svg"
                className="home-pasted-image4"
              />
              <img
                alt="pastedImage"
                src="/pastedimage-06e.svg"
                className="home-pasted-image5"
              />
            </div>
          </div>
        </div>
      </div>
      <div className="home-video1">
        <div className="home-content12">
          <div className="home-header1">
            <h2 className="home-text10">
              Built specifically for people who want faster transactions
            </h2>
          </div>
          <div className="home-video-container">
            <video
              src="https://www.youtube.com/watch?v=MRQ69XeDxVk"
              loop
              muted
              poster="/pastedimage-v2-900w.png"
              autoPlay
              className="home-video2"
            ></video>
            <div className="home-heading-container">
              <div className="home-heading11">
                <span className="home-text11">
                  Consectetur adipiscing elit, sed do eiusmod tempor
                  <span
                    dangerouslySetInnerHTML={{
                      __html: ' ',
                    }}
                  />
                </span>
                <span className="home-text12">
                  Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed
                  do eiusmod tempor incididunt.
                </span>
              </div>
              <div className="home-explore1">
                <span className="home-text13">Explore pricing plans -&gt;</span>
              </div>
            </div>
          </div>
        </div>
      </div>
      <div className="home-stats">
        <div className="home-stat1">
          <span className="home-caption4">200k</span>
          <span className="home-description1">
            Lorem ipsum dolor sit ametconsectetur adipiscing
            <span
              dangerouslySetInnerHTML={{
                __html: ' ',
              }}
            />
          </span>
        </div>
        <div className="home-stat2">
          <span className="home-caption5">$3,5 billions</span>
          <span className="home-description2">
            Lorem ipsum dolor sit ametconsectetur adipiscing
            <span
              dangerouslySetInnerHTML={{
                __html: ' ',
              }}
            />
          </span>
        </div>
        <div className="home-stat3">
          <span className="home-caption6">10.000 +</span>
          <span className="home-description3">
            Lorem ipsum dolor sit ametconsectetur adipiscing
            <span
              dangerouslySetInnerHTML={{
                __html: ' ',
              }}
            />
          </span>
        </div>
      </div>
      <div className="home-sections">
        <div className="home-section1">
          <div className="home-image1">
            <div className="home-image-highlight">
              <span className="home-text14">
                <span>
                  always know your in and out
                  <span
                    dangerouslySetInnerHTML={{
                      __html: ' ',
                    }}
                  />
                </span>
                <br></br>
              </span>
            </div>
          </div>
          <div className="home-content13">
            <h2 className="home-text17">Everything you get with Finbest</h2>
            <Highlight
              title="Lorem ipsum dolor sit amet, consectetur "
              description="Sed do eiusmod tempor incididunt ut labore et dolore magna aliquat enim ad minim veniam, quis nostrud"
            ></Highlight>
            <Highlight
              title="Lorem ipsum dolor sit amet, consectetur "
              description="Sed do eiusmod tempor incididunt ut labore et dolore"
            ></Highlight>
            <div className="home-explore2">
              <span>Explore pricing plans -&gt;</span>
            </div>
          </div>
        </div>
        <div className="home-section2">
          <div className="home-content14">
            <div className="home-heading12">
              <h2 className="home-text19">Keep track with all transactions</h2>
              <span className="home-text20">
                Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do
                eiusmod tempor incididunt.
              </span>
            </div>
            <div className="home-content15">
              <div className="home-points">
                <Point></Point>
                <Point text="Quis nostrud exercitation ullamco"></Point>
                <Point text="Reprehenderit qui in ea voluptate velit"></Point>
              </div>
              <Link to="/" className="home-navlink2">
                <div className="home-get-started1">
                  <span className="home-sign-up">Get started now</span>
                </div>
              </Link>
            </div>
          </div>
          <div className="home-image2"></div>
        </div>
        <div className="home-section3">
          <div className="home-image3">
            <div className="home-image-overlay"></div>
          </div>
          <div className="home-content16">
            <h2 className="home-text21">
              <span>Create milestones</span>
              <br></br>
            </h2>
            <Accordion></Accordion>
          </div>
        </div>
      </div>
      <div className="home-banner-container">
        <div className="home-banner">
          <div className="home-overlay1">
            <span className="home-text24">
              Begin your financial journey on finbest
            </span>
            <div className="home-book-btn">
              <span className="home-text25">Book a demo</span>
            </div>
          </div>
          <img
            alt="pastedImage"
            src="/pastedimage-ylke.svg"
            className="home-pasted-image6"
          />
        </div>
      </div>
      <div className="home-features">
        <div className="home-header2">
          <div className="home-tag1">
            <span className="home-text26">Features</span>
          </div>
          <div className="home-heading13">
            <h2 className="home-text27">Everything you get with Finbest</h2>
            <span className="home-text28">
              Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do
              eiusmod tempor incididunt.
            </span>
          </div>
        </div>
        <div className="home-feature-list">
          <Feature></Feature>
          <Feature
            title="Multiple Devices"
            thumbnail="/vector6113-r6dl.svg"
          ></Feature>
          <Feature title="Analytics" thumbnail="/vector6113-6zj.svg"></Feature>
          <Feature
            title="Virtual Card"
            thumbnail="/vector6113-lvvs.svg"
          ></Feature>
          <Feature
            title="Safe Transactions"
            thumbnail="/vector6114-cupp.svg"
          ></Feature>
          <Feature
            title="Milestones"
            thumbnail="/vector6114-6m1e.svg"
          ></Feature>
          <Feature title="Trade" thumbnail="/vector6114-yjl.svg"></Feature>
          <Feature title="Wallet" thumbnail="/vector6113-lvvs.svg"></Feature>
        </div>
      </div>
      <div className="home-pricing">
        <div className="home-content17">
          <div className="home-header3">
            <div className="home-tag2">
              <span className="home-text29">Pricing plans</span>
            </div>
            <div className="home-heading14">
              <h2 className="home-text30">No setup cost or hidden fees.</h2>
            </div>
          </div>
          <div className="home-pricing-plans">
            <div className="home-plans1">
              <div className="home-plan1">
                <div className="home-top1">
                  <div className="home-heading15">
                    <span className="home-text31">Standard</span>
                    <span className="home-text32">
                      Sed ut perspiciatis unde omnis iste natus error sit.
                    </span>
                  </div>
                  <div className="home-cost1">
                    <span className="home-text33">Free</span>
                  </div>
                </div>
                <div className="home-bottom1">
                  <div className="home-check-list1">
                    <Check></Check>
                    <Check feature="Quis nostrud exercitation ulla"></Check>
                    <Check feature="Duis aute irure dolor intuit"></Check>
                    <Check feature="Voluptas sit aspernatur aut odit"></Check>
                    <Check feature="Corporis suscipit laboriosam"></Check>
                  </div>
                  <div className="home-button1">
                    <span className="home-text34">Get Standard</span>
                  </div>
                </div>
              </div>
              <div className="home-plan2">
                <div className="home-top2">
                  <div className="home-heading16">
                    <span className="home-text35">Plus</span>
                    <span className="home-text36">
                      Sed ut perspiciatis unde omnis iste natus error sit.
                    </span>
                  </div>
                  <div className="home-cost2">
                    <span className="home-text37">$8</span>
                    <span className="home-text38">/month</span>
                  </div>
                </div>
                <div className="home-bottom2">
                  <div className="home-check-list2">
                    <Check></Check>
                    <Check feature="Quis nostrud exercitation ulla"></Check>
                    <Check feature="Duis aute irure dolor intuit"></Check>
                    <Check feature="Voluptas sit aspernatur aut odit"></Check>
                    <Check feature="Corporis suscipit laboriosam"></Check>
                  </div>
                  <div className="home-button2">
                    <span className="home-text39">Get Standard</span>
                  </div>
                </div>
              </div>
              <div className="home-plan3">
                <div className="home-top3">
                  <div className="home-heading17">
                    <span className="home-text40">Premium</span>
                    <span className="home-text41">
                      Sed ut perspiciatis unde omnis iste natus error sit.
                    </span>
                  </div>
                  <div className="home-cost3">
                    <span className="home-text42">$16</span>
                    <span className="home-text43">/month</span>
                  </div>
                </div>
                <div className="home-bottom3">
                  <div className="home-check-list3">
                    <Check></Check>
                    <Check feature="Quis nostrud exercitation ulla"></Check>
                    <Check feature="Duis aute irure dolor intuit"></Check>
                    <Check feature="Voluptas sit aspernatur aut odit"></Check>
                    <Check feature="Corporis suscipit laboriosam"></Check>
                  </div>
                  <div className="home-button3">
                    <span className="home-text44">Get Standard</span>
                  </div>
                </div>
              </div>
            </div>
            <div className="home-expand1">
              <div className="home-overlay2">
                <div className="home-header4">
                  <div className="home-heading18">
                    <span className="home-text45">Expand</span>
                    <span className="home-text46">
                      Lorem ipsum dolor sit amet, consectetur adipiscing elit,
                      sed do eiusmod tempor incididunt.
                    </span>
                  </div>
                  <div className="home-check-list4">
                    <div className="home-check1">
                      <svg viewBox="0 0 1024 1024" className="home-icon10">
                        <path d="M384 690l452-452 60 60-512 512-238-238 60-60z"></path>
                      </svg>
                      <span className="home-text47">
                        Sed ut perspiciatis unde
                      </span>
                    </div>
                    <div className="home-check2">
                      <svg viewBox="0 0 1024 1024" className="home-icon12">
                        <path d="M384 690l452-452 60 60-512 512-238-238 60-60z"></path>
                      </svg>
                      <span className="home-text48">
                        Quis nostrud exercitation ulla
                      </span>
                    </div>
                    <div className="home-check3">
                      <svg viewBox="0 0 1024 1024" className="home-icon14">
                        <path d="M384 690l452-452 60 60-512 512-238-238 60-60z"></path>
                      </svg>
                      <span className="home-text49">
                        Duis aute irure dolor intuit
                      </span>
                    </div>
                  </div>
                </div>
                <div className="home-button4">
                  <span className="home-text50">
                    <span>Contact us</span>
                    <br></br>
                  </span>
                </div>
              </div>
            </div>
          </div>
          <div className="home-plans2">
            <div className="home-plan4">
              <div className="home-top4">
                <div className="home-heading19">
                  <span className="home-text53">Standard</span>
                  <span className="home-text54">
                    Sed ut perspiciatis unde omnis iste natus error sit.
                  </span>
                </div>
                <div className="home-cost4">
                  <span className="home-text55">Free</span>
                </div>
              </div>
              <div className="home-bottom4">
                <div className="home-check-list5">
                  <Check></Check>
                  <Check feature="Quis nostrud exercitation ulla"></Check>
                  <Check feature="Duis aute irure dolor intuit"></Check>
                  <Check feature="Voluptas sit aspernatur aut odit"></Check>
                  <Check feature="Corporis suscipit laboriosam"></Check>
                </div>
                <div className="home-button5">
                  <span className="home-text56">Get Standard</span>
                </div>
              </div>
            </div>
            <div className="home-plan5">
              <div className="home-top5">
                <div className="home-heading20">
                  <span className="home-text57">Plus</span>
                  <span className="home-text58">
                    Sed ut perspiciatis unde omnis iste natus error sit.
                  </span>
                </div>
                <div className="home-cost5">
                  <span className="home-text59">$8</span>
                  <span className="home-text60">/month</span>
                </div>
              </div>
              <div className="home-bottom5">
                <div className="home-check-list6">
                  <Check></Check>
                  <Check feature="Quis nostrud exercitation ulla"></Check>
                  <Check feature="Duis aute irure dolor intuit"></Check>
                  <Check feature="Voluptas sit aspernatur aut odit"></Check>
                  <Check feature="Corporis suscipit laboriosam"></Check>
                </div>
                <div className="home-button6">
                  <span className="home-text61">Get Plus</span>
                </div>
              </div>
            </div>
            <div className="home-plan6">
              <div className="home-top6">
                <div className="home-heading21">
                  <span className="home-text62">Premium</span>
                  <span className="home-text63">
                    Sed ut perspiciatis unde omnis iste natus error sit.
                  </span>
                </div>
                <div className="home-cost6">
                  <span className="home-text64">$16</span>
                  <span className="home-text65">/month</span>
                </div>
              </div>
              <div className="home-bottom6">
                <div className="home-check-list7">
                  <Check></Check>
                  <Check feature="Quis nostrud exercitation ulla"></Check>
                  <Check feature="Duis aute irure dolor intuit"></Check>
                  <Check feature="Voluptas sit aspernatur aut odit"></Check>
                  <Check feature="Corporis suscipit laboriosam"></Check>
                </div>
                <div className="home-button7">
                  <span className="home-text66">Get Plus</span>
                </div>
              </div>
            </div>
            <div className="home-expand2">
              <div className="home-overlay3">
                <div className="home-header5">
                  <div className="home-heading22">
                    <span className="home-text67">Expand</span>
                    <span className="home-text68">
                      Lorem ipsum dolor sit amet, consectetur adipiscing elit,
                      sed do eiusmod tempor incididunt.
                    </span>
                  </div>
                  <div className="home-check-list8">
                    <div className="home-check4">
                      <svg viewBox="0 0 1024 1024" className="home-icon16">
                        <path d="M384 690l452-452 60 60-512 512-238-238 60-60z"></path>
                      </svg>
                      <span className="home-text69">
                        Sed ut perspiciatis unde
                      </span>
                    </div>
                    <div className="home-check5">
                      <svg viewBox="0 0 1024 1024" className="home-icon18">
                        <path d="M384 690l452-452 60 60-512 512-238-238 60-60z"></path>
                      </svg>
                      <span className="home-text70">
                        Quis nostrud exercitation ulla
                      </span>
                    </div>
                    <div className="home-check6">
                      <svg viewBox="0 0 1024 1024" className="home-icon20">
                        <path d="M384 690l452-452 60 60-512 512-238-238 60-60z"></path>
                      </svg>
                      <span className="home-text71">
                        Duis aute irure dolor intuit
                      </span>
                    </div>
                  </div>
                </div>
                <div className="home-button8">
                  <span className="home-text72">
                    <span>Contact us</span>
                    <br></br>
                  </span>
                </div>
              </div>
            </div>
          </div>
        </div>
        <div className="home-help">
          <span className="home-text75">Need any help?</span>
          <div className="home-explore3">
            <span className="home-text76">
              Get in touch with us right away -&gt;
            </span>
          </div>
        </div>
      </div>
      <div className="home-testimonials">
        <div className="home-logo-container">
          <img
            alt="pastedImage"
            src="/pastedimage-idcu.svg"
            className="home-logo"
          />
        </div>
        <div className="home-content18">
          <div id="quotes" className="home-quotes">
            <div className="quote active-quote">
              <Quote rootClassName="quoteroot-class-name"></Quote>
            </div>
            <div className="quote">
              <Quote
                quote='"Testing these templates is a pleasure."'
                title="Developer @ Vista La Vista"
                author="Author 2"
                rootClassName="quoteroot-class-name"
              ></Quote>
            </div>
            <div className="quote">
              <Quote
                quote='"Wow, awesome works!'
                title="Designer @ OhBoy"
                rootClassName="quoteroot-class-name"
              ></Quote>
            </div>
          </div>
          <div className="home-buttons">
            <div id="quote-before" className="home-left">
              <svg viewBox="0 0 1024 1024" className="home-icon22">
                <path d="M854 470v84h-520l238 240-60 60-342-342 342-342 60 60-238 240h520z"></path>
              </svg>
            </div>
            <div id="quote-next" className="home-right">
              <svg viewBox="0 0 1024 1024" className="home-icon24">
                <path d="M512 170l342 342-342 342-60-60 238-240h-520v-84h520l-238-240z"></path>
              </svg>
            </div>
          </div>
          <div>
            <div className="home-container3">
              <Script
                html={` <script>
    /* Quote Slider - Code Embed */

    let current = 1;

    const nextButton = document.querySelector("#quote-next");
    const previousButton = document.querySelector("#quote-before");
    const quotes = document.querySelectorAll(".quote");

    if (nextButton && previousButton && quotes) {
      nextButton.addEventListener("click", () => {
        quotes.forEach((quote) => {
          quote.classList.remove("active-quote");
        });

        current == quotes.length ? (current = 1) : current++;
        quotes[current - 1].classList.add("active-quote");
      });

      previousButton.addEventListener("click", () => {
        quotes.forEach((quote) => {
          quote.classList.remove("active-quote");
        });

        current == 1 ? (current = quotes.length) : current--;
        quotes[current - 1].classList.add("active-quote");
      });
    }
  </script>`}
              ></Script>
            </div>
          </div>
        </div>
      </div>
      <div className="home-faq">
        <div className="home-content19">
          <div className="home-header6">
            <div className="home-tag3">
              <span className="home-text77">
                <span>FAQ</span>
                <br></br>
              </span>
            </div>
            <div className="home-heading23">
              <h2 className="home-text80">Frequently Asked Questions</h2>
            </div>
          </div>
          <div className="home-rows">
            <div className="home-column4">
              <div className="Question">
                <span className="home-title2">
                  What is sit amet, consectetur adipiscing elit, sed do?
                  <span
                    dangerouslySetInnerHTML={{
                      __html: ' ',
                    }}
                  />
                </span>
                <span className="home-description4">
                  Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed
                  do eiusmod tempor incididunt ut labore et dolore magna aliqua.
                  Excepteur sint occaecat cupidatat non proident, sunt in culpa
                  qui officia deserunt mollit anim id est laborum.
                  <span
                    dangerouslySetInnerHTML={{
                      __html: ' ',
                    }}
                  />
                </span>
              </div>
              <div className="Question">
                <span className="home-title3">
                  What is sit amet, consectetur adipiscing elit, sed do?
                  <span
                    dangerouslySetInnerHTML={{
                      __html: ' ',
                    }}
                  />
                </span>
                <span className="home-description5">
                  <span>
                    Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed
                    do eiusmod tempor incididunt ut labore et dolore magna
                    aliqua. Excepteur sint occaecat cupidatat non proident, sunt
                    in culpa qui officia deserunt mollit anim id est laborum.
                    <span
                      dangerouslySetInnerHTML={{
                        __html: ' ',
                      }}
                    />
                  </span>
                  <br></br>
                  <span>
                    tempor incididunt ut labore et dolore magna aliqua.
                    Excepteur sint occaecat
                    <span
                      dangerouslySetInnerHTML={{
                        __html: ' ',
                      }}
                    />
                  </span>
                </span>
              </div>
              <div className="home-question3 Question">
                <span className="home-title4">
                  What is sit amet, consectetur adipiscing elit, sed do?
                  <span
                    dangerouslySetInnerHTML={{
                      __html: ' ',
                    }}
                  />
                </span>
                <span className="home-description6">
                  Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed
                  do eiusmod tempor incididunt ut labore et dolore magna.
                </span>
              </div>
            </div>
            <div className="home-column5">
              <div className="home-question4 Question">
                <span className="home-title5">
                  What is sit amet, consectetur adipiscing elit, sed do?
                  <span
                    dangerouslySetInnerHTML={{
                      __html: ' ',
                    }}
                  />
                </span>
                <span className="home-description7">
                  Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed
                  do eiusmod tempor incididunt ut labore et dolore magna.
                </span>
              </div>
              <div className="home-question5 Question">
                <span className="home-title6">
                  What is sit amet, consectetur adipiscing elit, sed do?
                  <span
                    dangerouslySetInnerHTML={{
                      __html: ' ',
                    }}
                  />
                </span>
                <span className="home-description8">
                  Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed
                  do eiusmod tempor incididunt ut labore et dolore magna.
                </span>
              </div>
              <div className="home-question6 Question">
                <span className="home-title7">
                  What is sit amet, consectetur adipiscing elit, sed do?
                  <span
                    dangerouslySetInnerHTML={{
                      __html: ' ',
                    }}
                  />
                </span>
                <span className="home-description9">
                  <span>
                    Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed
                    do eiusmod tempor incididunt ut labore et dolore magna
                    aliqua. Excepteur sint occaecat cupidatat non proident, sunt
                    in culpa qui officia deserunt mollit anim id est laborum.
                    <span
                      dangerouslySetInnerHTML={{
                        __html: ' ',
                      }}
                    />
                  </span>
                  <br></br>
                  <span>
                    tempor incididunt ut labore et dolore magna aliqua.
                    Excepteur sint occaecat
                    <span
                      dangerouslySetInnerHTML={{
                        __html: ' ',
                      }}
                    />
                  </span>
                </span>
              </div>
            </div>
          </div>
        </div>
      </div>
      <div className="home-get-started2">
        <div className="home-content20">
          <div className="home-heading24">
            <h2 className="home-text87">Get started with finbest now!</h2>
            <span className="home-text88">
              Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do
              eiusmod tempor incididunt ut labore magna.
            </span>
          </div>
          <div className="home-hero-buttons2">
            <div className="home-ios-btn2">
              <img
                alt="pastedImage"
                src="/pastedimage-zmzg.svg"
                className="home-apple2"
              />
              <span className="home-caption7">Download for iOS</span>
            </div>
            <div className="home-android-btn2">
              <img
                alt="pastedImage"
                src="/pastedimage-ld65.svg"
                className="home-android2"
              />
              <span className="home-caption8">Download for Android</span>
            </div>
          </div>
        </div>
      </div>
      <Footer></Footer>
    </div>
  )
}

export default Home
